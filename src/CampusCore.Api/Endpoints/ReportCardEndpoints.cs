using CampusCore.Application.Reports;

namespace CampusCore.Api.Endpoints;

public static class ReportCardEndpoints
{
    public static IEndpointRouteBuilder MapReportCardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/reports/report-card/{studentId:guid}/{academicYearId:guid}", async (
            Guid studentId,
            Guid academicYearId,
            ReportCardService service,
            CancellationToken ct) =>
        {
            var report = await service.GetAsync(studentId, academicYearId, ct);
            return report is null ? Results.NotFound() : Results.Ok(report);
        }).WithTags("Reports").RequireAuthorization();

        return endpoints;
    }
}
