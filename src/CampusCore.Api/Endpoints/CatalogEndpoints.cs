using CampusCore.Application.Abstractions;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Endpoints;

public static class CatalogEndpoints
{
    public sealed record AcademicYearRequest(string Name, DateOnly StartsOn, DateOnly EndsOn, bool IsActive);
    public sealed record ClassRequest(string Name, int SortOrder);
    public sealed record SectionRequest(Guid SchoolClassId, string Name, int Capacity);
    public sealed record SubjectRequest(string Code, string Name, decimal MaximumMarks);
    public sealed record GradeScaleRequest(string Name, decimal MinimumPercentage, decimal MaximumPercentage, string Grade, string? Description);

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/catalog").WithTags("Catalog").RequireAuthorization();
        group.MapGet("/academic-years", async (IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.AcademicYears.AsNoTracking().OrderByDescending(x => x.StartsOn).ToListAsync(ct)));
        group.MapGet("/classes", async (IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.SchoolClasses.AsNoTracking().Include(x => x.Sections).OrderBy(x => x.SortOrder).ToListAsync(ct)));
        group.MapGet("/subjects", async (IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.Subjects.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct)));
        group.MapGet("/grade-scales", async (IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.GradeScales.AsNoTracking().OrderByDescending(x => x.MinimumPercentage).ToListAsync(ct)));

        group.MapPost("/academic-years", async (AcademicYearRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) => { var x = new AcademicYear { Name = r.Name.Trim(), StartsOn = r.StartsOn, EndsOn = r.EndsOn, IsActive = r.IsActive }; x.Validate(); db.AcademicYears.Add(x); await db.SaveChangesAsync(ct); await audit.WriteAsync("academic_year.created", nameof(AcademicYear), x.Id.ToString(), new { x.Name, x.StartsOn, x.EndsOn }, ct); return Results.Created($"/api/catalog/academic-years/{x.Id}", new { x.Id }); }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));
        group.MapPost("/classes", async (ClassRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) => { if (string.IsNullOrWhiteSpace(r.Name)) return Results.BadRequest(); var x = new SchoolClass { Name = r.Name.Trim(), SortOrder = r.SortOrder }; db.SchoolClasses.Add(x); await db.SaveChangesAsync(ct); await audit.WriteAsync("class.created", nameof(SchoolClass), x.Id.ToString(), new { x.Name, x.SortOrder }, ct); return Results.Created($"/api/catalog/classes/{x.Id}", new { x.Id }); }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));
        group.MapPost("/sections", async (SectionRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) => { if (r.Capacity is < 1 or > 500 || string.IsNullOrWhiteSpace(r.Name) || !await db.SchoolClasses.AnyAsync(x => x.Id == r.SchoolClassId, ct)) return Results.BadRequest(new { message = "Section name, class, and capacity (1-500) are required." }); var x = new Section { SchoolClassId = r.SchoolClassId, Name = r.Name.Trim(), Capacity = r.Capacity }; db.Sections.Add(x); await db.SaveChangesAsync(ct); await audit.WriteAsync("section.created", nameof(Section), x.Id.ToString(), new { x.SchoolClassId, x.Name, x.Capacity }, ct); return Results.Created($"/api/catalog/sections/{x.Id}", new { x.Id }); }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));
        group.MapPost("/subjects", async (SubjectRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) => { if (r.MaximumMarks <= 0 || string.IsNullOrWhiteSpace(r.Code) || string.IsNullOrWhiteSpace(r.Name)) return Results.BadRequest(); var x = new Subject { Code = r.Code.Trim().ToUpperInvariant(), Name = r.Name.Trim(), MaximumMarks = r.MaximumMarks }; db.Subjects.Add(x); await db.SaveChangesAsync(ct); await audit.WriteAsync("subject.created", nameof(Subject), x.Id.ToString(), new { x.Code, x.Name }, ct); return Results.Created($"/api/catalog/subjects/{x.Id}", new { x.Id }); }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));
        group.MapPost("/grade-scales", async (GradeScaleRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) => { if (r.MinimumPercentage < 0 || r.MaximumPercentage > 100 || r.MaximumPercentage < r.MinimumPercentage || string.IsNullOrWhiteSpace(r.Grade)) return Results.BadRequest(); var x = new GradeScale { Name = r.Name.Trim(), MinimumPercentage = r.MinimumPercentage, MaximumPercentage = r.MaximumPercentage, Grade = r.Grade.Trim(), Description = r.Description?.Trim() }; db.GradeScales.Add(x); await db.SaveChangesAsync(ct); await audit.WriteAsync("grade_scale.created", nameof(GradeScale), x.Id.ToString(), new { x.Name, x.MinimumPercentage, x.MaximumPercentage, x.Grade }, ct); return Results.Created($"/api/catalog/grade-scales/{x.Id}", new { x.Id }); }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));
        return endpoints;
    }
}
