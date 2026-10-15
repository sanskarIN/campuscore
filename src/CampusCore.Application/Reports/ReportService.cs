using System.Text;
using CampusCore.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Application.Reports;

public sealed class ReportService(IApplicationDbContext db)
{
    public async Task<byte[]> ExportStudentsCsvAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Students.AsNoTracking().OrderBy(x => x.AdmissionNumber)
            .Select(x => new { x.AdmissionNumber, x.FirstName, x.LastName, x.DateOfBirth, x.Email, x.Phone, x.IsActive })
            .ToListAsync(cancellationToken);
        var sb = new StringBuilder("AdmissionNumber,FirstName,LastName,DateOfBirth,Email,Phone,IsActive\r\n");
        foreach (var row in rows)
        {
            sb.Append(Csv(row.AdmissionNumber)).Append(',').Append(Csv(row.FirstName)).Append(',').Append(Csv(row.LastName)).Append(',')
              .Append(row.DateOfBirth.ToString("yyyy-MM-dd")).Append(',').Append(Csv(row.Email)).Append(',').Append(Csv(row.Phone)).Append(',').Append(row.IsActive).Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    internal static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        var significant = text.TrimStart(' ', '\t', '\r', '\n');
        if (significant.Length > 0 && significant[0] is '=' or '+' or '-' or '@')
            text = "'" + text;
        return '"' + text.Replace("\"", "\"\"") + '"';
    }
}
