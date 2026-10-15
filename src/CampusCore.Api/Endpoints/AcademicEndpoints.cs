using CampusCore.Application.Academics;
using CampusCore.Infrastructure.Identity;

namespace CampusCore.Api.Endpoints;

public static class AcademicEndpoints
{
    public static IEndpointRouteBuilder MapAcademicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/academics").WithTags("Academics").RequireAuthorization();
        group.MapPut("/attendance", async (AttendanceUpsert request, AcademicService service, CancellationToken ct) =>
        {
            await service.UpsertAttendanceAsync(request, ct);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar, CampusRoles.Teacher));
        group.MapPost("/marks", async (MarkUpsert request, AcademicService service, CancellationToken ct) =>
            Results.Created("/api/academics/marks", new { id = await service.RecordMarkAsync(request, ct) }))
            .RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Teacher));
        group.MapGet("/grades/resolve", async (decimal percentage, string? scaleName, AcademicService service, CancellationToken ct) =>
            await service.ResolveGradeAsync(percentage, scaleName, ct) is { } grade ? Results.Ok(grade) : Results.NotFound());
        return endpoints;
    }
}
