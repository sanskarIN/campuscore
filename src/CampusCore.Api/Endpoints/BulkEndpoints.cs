using System.Globalization;
using System.Text;
using CampusCore.Application.Abstractions;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Identity;
using CampusCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Endpoints;

public static class BulkEndpoints
{
    private const long MaxImportBytes = 2 * 1024 * 1024;
    private const int MaxImportRows = 5_000;
    private const int MaxBulkStatusStudents = 500;

    public sealed record ImportIssue(int Row, string Field, string Message);
    public sealed record ImportPreview(int TotalRows, int ValidRows, IReadOnlyList<ImportIssue> Issues);
    public sealed record BulkStatusRequest(IReadOnlyList<Guid> StudentIds, bool IsActive);

    public static IEndpointRouteBuilder MapBulkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/bulk").WithTags("Bulk operations")
            .RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));

        group.MapPost("/students/preview", async (IFormFile file, IApplicationDbContext db, CancellationToken ct) =>
        {
            var parsed = await ParseAndValidateAsync(file, db, ct);
            return parsed.Error is not null ? parsed.Error : Results.Ok(parsed.Preview);
        }).DisableAntiforgery();

        group.MapPost("/students/commit", async (IFormFile file, ApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            var parsed = await ParseAndValidateAsync(file, db, ct);
            if (parsed.Error is not null) return parsed.Error;
            if (parsed.Preview!.Issues.Count > 0)
                return Results.BadRequest(new { message = "Import contains validation errors.", preview = parsed.Preview });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.Students.AddRange(parsed.Rows!.Select(row => new Student
            {
                AdmissionNumber = row.AdmissionNumber,
                FirstName = row.FirstName,
                LastName = row.LastName,
                DateOfBirth = row.DateOfBirth,
                Email = EmptyToNull(row.Email),
                Phone = EmptyToNull(row.Phone),
                AddressLine = EmptyToNull(row.AddressLine)
            }));
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("students.bulk_imported", nameof(Student), "batch", new { Count = parsed.Rows.Count }, ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { imported = parsed.Rows.Count });
        }).DisableAntiforgery();

        group.MapPatch("/students/status", async (BulkStatusRequest request, ApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (request.StudentIds is null || request.StudentIds.Count == 0)
                return Results.BadRequest(new { message = "At least one student is required." });
            if (request.StudentIds.Count > MaxBulkStatusStudents)
                return Results.BadRequest(new { message = $"A maximum of {MaxBulkStatusStudents} students can be updated in one request." });

            var ids = request.StudentIds.Distinct().ToArray();
            if (ids.Length == 0) return Results.BadRequest(new { message = "At least one student is required." });
            var students = await db.Students.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            if (students.Count != ids.Length) return Results.BadRequest(new { message = "One or more student identifiers are invalid." });

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            foreach (var student in students) student.IsActive = request.IsActive;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("students.bulk_status_updated", nameof(Student), "batch", new { Count = students.Count, request.IsActive }, ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { updated = students.Count });
        });

        return endpoints;
    }

    private static async Task<ParseResult> ParseAndValidateAsync(IFormFile file, IApplicationDbContext db, CancellationToken ct)
    {
        if (file.Length <= 0 || file.Length > MaxImportBytes)
            return ParseResult.FromError(Results.BadRequest(new { message = $"CSV file must be between 1 byte and {MaxImportBytes} bytes." }));
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            return ParseResult.FromError(Results.BadRequest(new { message = "Only .csv student imports are supported." }));

        List<string[]> records;
        await using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false))
        {
            try { records = Csv.Read(await reader.ReadToEndAsync(ct), ct); }
            catch (FormatException ex) { return ParseResult.FromError(Results.BadRequest(new { message = ex.Message })); }
        }

        if (records.Count == 0) return ParseResult.FromError(Results.BadRequest(new { message = "CSV is empty." }));
        if (records.Count - 1 > MaxImportRows)
            return ParseResult.FromError(Results.BadRequest(new { message = $"CSV imports are limited to {MaxImportRows} data rows." }));
        var expected = new[] { "AdmissionNumber", "FirstName", "LastName", "DateOfBirth", "Email", "Phone", "AddressLine" };
        var header = records[0].Select(x => x.Trim()).ToArray();
        if (!header.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            return ParseResult.FromError(Results.BadRequest(new { message = $"CSV header must be: {string.Join(',', expected)}" }));

        var rows = new List<StudentImportRow>();
        var issues = new List<ImportIssue>();
        var admissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < records.Count; index++)
        {
            var record = records[index];
            var displayRow = index + 1;
            if (record.Length != expected.Length)
            {
                issues.Add(new ImportIssue(displayRow, "row", $"Expected {expected.Length} columns but found {record.Length}."));
                continue;
            }

            var admission = record[0].Trim();
            var firstName = record[1].Trim();
            var lastName = record[2].Trim();
            var email = record[4].Trim();
            var phone = record[5].Trim();
            var address = record[6].Trim();

            if (string.IsNullOrWhiteSpace(admission)) issues.Add(new ImportIssue(displayRow, "AdmissionNumber", "Admission number is required."));
            else if (admission.Length > 64) issues.Add(new ImportIssue(displayRow, "AdmissionNumber", "Admission number cannot exceed 64 characters."));
            if (string.IsNullOrWhiteSpace(firstName)) issues.Add(new ImportIssue(displayRow, "FirstName", "First name is required."));
            else if (firstName.Length > 120) issues.Add(new ImportIssue(displayRow, "FirstName", "First name cannot exceed 120 characters."));
            if (lastName.Length > 120) issues.Add(new ImportIssue(displayRow, "LastName", "Last name cannot exceed 120 characters."));
            if (email.Length > 254) issues.Add(new ImportIssue(displayRow, "Email", "Email cannot exceed 254 characters."));
            if (phone.Length > 40) issues.Add(new ImportIssue(displayRow, "Phone", "Phone cannot exceed 40 characters."));
            if (address.Length > 1000) issues.Add(new ImportIssue(displayRow, "AddressLine", "Address cannot exceed 1000 characters."));

            if (!DateOnly.TryParseExact(record[3].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOfBirth))
                issues.Add(new ImportIssue(displayRow, "DateOfBirth", "Use yyyy-MM-dd."));
            else if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
                issues.Add(new ImportIssue(displayRow, "DateOfBirth", "Date of birth cannot be in the future."));
            if (!string.IsNullOrWhiteSpace(admission) && !admissions.Add(admission))
                issues.Add(new ImportIssue(displayRow, "AdmissionNumber", "Admission number is duplicated in this file."));

            if (issues.Any(x => x.Row == displayRow)) continue;
            rows.Add(new StudentImportRow(admission, firstName, lastName, dateOfBirth, email, phone, address));
        }

        if (admissions.Count > 0)
        {
            var normalized = admissions.Select(x => x.ToLower()).ToArray();
            var existing = await db.Students.AsNoTracking().Where(x => normalized.Contains(x.AdmissionNumber.ToLower())).Select(x => x.AdmissionNumber).ToListAsync(ct);
            foreach (var admission in existing)
            {
                var row = rows.FindIndex(x => string.Equals(x.AdmissionNumber, admission, StringComparison.OrdinalIgnoreCase));
                issues.Add(new ImportIssue(row < 0 ? 0 : row + 2, "AdmissionNumber", $"Admission number '{admission}' already exists."));
            }
        }

        var preview = new ImportPreview(
            Math.Max(0, records.Count - 1),
            Math.Max(0, records.Count - 1 - issues.Select(x => x.Row).Where(x => x > 0).Distinct().Count()),
            issues.OrderBy(x => x.Row).ThenBy(x => x.Field).ToList());
        return new ParseResult(rows, preview, null);
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed record StudentImportRow(string AdmissionNumber, string FirstName, string LastName, DateOnly DateOfBirth, string Email, string Phone, string AddressLine);
    private sealed record ParseResult(IReadOnlyList<StudentImportRow>? Rows, ImportPreview? Preview, IResult? Error)
    {
        public static ParseResult FromError(IResult result) => new(null, null, result);
    }

    private static class Csv
    {
        public static List<string[]> Read(string text, CancellationToken cancellationToken)
        {
            var records = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            for (var index = 0; index < text.Length; index++)
            {
                if ((index & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var ch = text[index];
                if (quoted)
                {
                    if (ch == '"')
                    {
                        if (index + 1 < text.Length && text[index + 1] == '"') { field.Append('"'); index++; }
                        else quoted = false;
                    }
                    else field.Append(ch);
                    continue;
                }

                if (ch == '"' && field.Length == 0) { quoted = true; continue; }
                if (ch == ',') { row.Add(field.ToString()); field.Clear(); continue; }
                if (ch is '\r' or '\n')
                {
                    if (ch == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                    row.Add(field.ToString()); field.Clear();
                    if (row.Any(x => x.Length > 0)) records.Add(row.ToArray());
                    row.Clear();
                    continue;
                }
                field.Append(ch);
            }
            if (quoted) throw new FormatException("CSV contains an unclosed quoted field.");
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); records.Add(row.ToArray()); }
            return records;
        }
    }
}
