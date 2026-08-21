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
        endpoints.MapGet("/api/dashboard", async (DashboardService service, CancellationToken ct) => Results.Ok(await service.GetAsync(DateOnly.FromDateTime(DateTime.UtcNow), ct))).RequireAuthorization();
        endpoints.MapGet("/api/reports/students.csv", async (ReportService service, CancellationToken ct) => Results.File(await service.ExportStudentsCsvAsync(ct), "text/csv; charset=utf-8", "students.csv")).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));

        var group = endpoints.MapGroup("/api/admin").WithTags("Administration").RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));
        group.MapGet("/audit", async (int page, int pageSize, IApplicationDbContext db, CancellationToken ct) =>
        {
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize == 0 ? 50 : pageSize, 1, 100);
            var total = await db.AuditLogs.CountAsync(ct);
            var items = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { items, page, pageSize, total });
        });
        group.MapGet("/settings", async (IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.InstitutionSettings.AsNoTracking().SingleAsync(ct)));
        group.MapPut("/settings", async (SettingsRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.InstitutionName) || request.DefaultPageSize is < 10 or > 100) return Results.BadRequest();
            var x = await db.InstitutionSettings.SingleAsync(ct);
            x.InstitutionName = request.InstitutionName.Trim(); x.Address = request.Address?.Trim(); x.TimeZoneId = request.TimeZoneId?.Trim(); x.Locale = request.Locale.Trim(); x.DateFormat = request.DateFormat.Trim(); x.DefaultPageSize = request.DefaultPageSize; x.AllowGuardianPortal = request.AllowGuardianPortal;
            await db.SaveChangesAsync(ct); await audit.WriteAsync("settings.updated", nameof(InstitutionSettings), x.Id.ToString(), new { x.Locale, x.DefaultPageSize, x.AllowGuardianPortal }, ct); return Results.NoContent();
        });
        return endpoints;
    }
}
