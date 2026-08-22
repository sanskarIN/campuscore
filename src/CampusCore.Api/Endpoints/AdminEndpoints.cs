using System.Globalization;
using CampusCore.Api.Validation;
using CampusCore.Application.Abstractions;
using CampusCore.Application.Dashboard;
using CampusCore.Application.Reports;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Endpoints;

public static class AdminEndpoints
{
    public sealed record SettingsRequest(string InstitutionName, string? Address, string? TimeZoneId, string Locale, string DateFormat, int DefaultPageSize, bool AllowGuardianPortal);

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", async (DashboardService service, CancellationToken ct) => Results.Ok(await service.GetCurrentAsync(ct))).RequireAuthorization();
        endpoints.MapGet("/api/reports/students.csv", async (ReportService service, CancellationToken ct) => Results.File(await service.ExportStudentsCsvAsync(ct), "text/csv; charset=utf-8", "students.csv")).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));

        var group = endpoints.MapGroup("/api/admin").WithTags("Administration").RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));
        group.MapGet("/audit", async (int page, int pageSize, IApplicationDbContext db, CancellationToken ct) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 100);
            var total = await db.AuditLogs.CountAsync(ct);
            var items = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        });
        group.MapGet("/settings", async (IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.InstitutionSettings.AsNoTracking().SingleAsync(ct)));
        group.MapPut("/settings", async (SettingsRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (request.DefaultPageSize is < 10 or > 100) return Results.BadRequest(new { message = "Default page size must be between 10 and 100." });

            var institutionName = RequestText.Required(request.InstitutionName, "Institution name", 200);
            var address = RequestText.Optional(request.Address, "Address", 1000);
            var timeZoneId = RequestText.Optional(request.TimeZoneId, "Time zone", 100) ?? "UTC";
            var locale = RequestText.Required(request.Locale, "Locale", 35);
            var dateFormat = RequestText.Required(request.DateFormat, "Date format", 64);

            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return Results.BadRequest(new { message = "Time zone identifier is not supported by this deployment." });
            }
            catch (InvalidTimeZoneException)
            {
                return Results.BadRequest(new { message = "Time zone identifier is invalid." });
            }

            try
            {
                _ = CultureInfo.GetCultureInfo(locale);
            }
            catch (CultureNotFoundException)
            {
                return Results.BadRequest(new { message = "Locale is not supported by this deployment." });
            }

            var x = await db.InstitutionSettings.SingleAsync(ct);
            x.InstitutionName = institutionName;
            x.Address = address;
            x.TimeZoneId = timeZoneId;
            x.Locale = locale;
            x.DateFormat = dateFormat;
            x.DefaultPageSize = request.DefaultPageSize;
            x.AllowGuardianPortal = request.AllowGuardianPortal;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("settings.updated", nameof(InstitutionSettings), x.Id.ToString(), new { x.Locale, x.DefaultPageSize, x.AllowGuardianPortal }, ct);
            return Results.NoContent();
        });
        return endpoints;
    }
}
