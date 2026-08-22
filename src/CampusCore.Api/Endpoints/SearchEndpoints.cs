using CampusCore.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/search", async (string q, IApplicationDbContext db, CancellationToken ct) =>
        {
            q = (q ?? string.Empty).Trim();
            if (q.Length < 2) return Results.Ok(Array.Empty<object>());
            if (q.Length > 120) return Results.BadRequest(new { message = "Search query cannot exceed 120 characters." });
            var lower = q.ToLower();
            var students = await db.Students.AsNoTracking()
                .Where(x => x.AdmissionNumber.ToLower().Contains(lower) || x.FirstName.ToLower().Contains(lower) || x.LastName.ToLower().Contains(lower))
                .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                .Take(8)
                .Select(x => new { type = "student", id = x.Id, title = (x.FirstName + " " + x.LastName).Trim(), subtitle = x.AdmissionNumber })
                .ToListAsync(ct);
            var staff = await db.StaffMembers.AsNoTracking()
                .Where(x => x.EmployeeNumber.ToLower().Contains(lower) || x.FirstName.ToLower().Contains(lower) || x.LastName.ToLower().Contains(lower))
                .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                .Take(8)
                .Select(x => new { type = "staff", id = x.Id, title = (x.FirstName + " " + x.LastName).Trim(), subtitle = x.EmployeeNumber })
                .ToListAsync(ct);
            return Results.Ok(students.Cast<object>().Concat(staff).Take(12));
        }).RequireAuthorization();
        return endpoints;
    }
}
