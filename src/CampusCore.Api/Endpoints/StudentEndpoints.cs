using CampusCore.Application.Students;
using CampusCore.Infrastructure.Identity;

namespace CampusCore.Api.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/students").WithTags("Students").RequireAuthorization();
        group.MapGet("/", async (string? q, Guid? sectionId, bool? active, int page, int pageSize, StudentService service, CancellationToken ct) =>
            Results.Ok(await service.SearchAsync(q, sectionId, active, page == 0 ? 1 : page, pageSize == 0 ? 25 : pageSize, ct)));
        group.MapGet("/{id:guid}", async (Guid id, StudentService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } student ? Results.Ok(student) : Results.NotFound());
        group.MapPost("/", async (CreateStudentRequest request, StudentService service, CancellationToken ct) =>
            Results.Created($"/api/students/{await service.CreateAsync(request, ct)}", null))
            .RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));
        group.MapPut("/{id:guid}", async (Guid id, UpdateStudentRequest request, StudentService service, CancellationToken ct) =>
            await service.UpdateAsync(id, request, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));
        group.MapPost("/{id:guid}/guardians", async (Guid id, UpsertGuardianRequest request, StudentService service, CancellationToken ct) =>
        {
            var guardianId = await service.AddGuardianAsync(id, request, ct);
            return guardianId is null ? Results.NotFound() : Results.Created($"/api/students/{id}", new { id = guardianId });
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));
        return endpoints;
    }
}
