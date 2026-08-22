using CampusCore.Api.Validation;
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
        group.MapGet("/grade-scales", async (IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.GradeScales.AsNoTracking().OrderBy(x => x.Name).ThenByDescending(x => x.MinimumPercentage).ToListAsync(ct)));

        group.MapPost("/academic-years", async (AcademicYearRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            var x = new AcademicYear
            {
                Name = RequestText.Required(r.Name, "Academic year name", 64),
                StartsOn = r.StartsOn,
                EndsOn = r.EndsOn,
                IsActive = r.IsActive
            };
            x.Validate();
            db.AcademicYears.Add(x);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("academic_year.created", nameof(AcademicYear), x.Id.ToString(), new { x.Name, x.StartsOn, x.EndsOn }, ct);
            return Results.Created($"/api/catalog/academic-years/{x.Id}", new { x.Id });
        }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));

        group.MapPost("/classes", async (ClassRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            var name = RequestText.Required(r.Name, "Class name", 120);
            var x = new SchoolClass { Name = name, SortOrder = r.SortOrder };
            db.SchoolClasses.Add(x);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("class.created", nameof(SchoolClass), x.Id.ToString(), new { x.Name, x.SortOrder }, ct);
            return Results.Created($"/api/catalog/classes/{x.Id}", new { x.Id });
        }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));

        group.MapPost("/sections", async (SectionRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (r.Capacity is < 1 or > 500 || !await db.SchoolClasses.AnyAsync(x => x.Id == r.SchoolClassId, ct))
                return Results.BadRequest(new { message = "Class and capacity (1-500) are required." });
            var name = RequestText.Required(r.Name, "Section name", 120);
            var x = new Section { SchoolClassId = r.SchoolClassId, Name = name, Capacity = r.Capacity };
            db.Sections.Add(x);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("section.created", nameof(Section), x.Id.ToString(), new { x.SchoolClassId, x.Name, x.Capacity }, ct);
            return Results.Created($"/api/catalog/sections/{x.Id}", new { x.Id });
        }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));

        group.MapPost("/subjects", async (SubjectRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (r.MaximumMarks <= 0) return Results.BadRequest(new { message = "Maximum marks must be greater than zero." });
            var code = RequestText.Required(r.Code, "Subject code", 32).ToUpperInvariant();
            var name = RequestText.Required(r.Name, "Subject name", 160);
            var x = new Subject { Code = code, Name = name, MaximumMarks = r.MaximumMarks };
            db.Subjects.Add(x);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("subject.created", nameof(Subject), x.Id.ToString(), new { x.Code, x.Name }, ct);
            return Results.Created($"/api/catalog/subjects/{x.Id}", new { x.Id });
        }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));

        group.MapPost("/grade-scales", async (GradeScaleRequest r, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (r.MinimumPercentage < 0 || r.MaximumPercentage > 100 || r.MaximumPercentage < r.MinimumPercentage)
                return Results.BadRequest(new { message = "Grade percentages must define a range between 0 and 100." });
            var name = RequestText.Required(r.Name, "Grade scale name", 120);
            var grade = RequestText.Required(r.Grade, "Grade", 16);
            var description = RequestText.Optional(r.Description, "Grade description", 500);
            var x = new GradeScale { Name = name, MinimumPercentage = r.MinimumPercentage, MaximumPercentage = r.MaximumPercentage, Grade = grade, Description = description };
            db.GradeScales.Add(x);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("grade_scale.created", nameof(GradeScale), x.Id.ToString(), new { x.Name, x.MinimumPercentage, x.MaximumPercentage, x.Grade }, ct);
            return Results.Created($"/api/catalog/grade-scales/{x.Id}", new { x.Id });
        }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator));
        return endpoints;
    }
}
