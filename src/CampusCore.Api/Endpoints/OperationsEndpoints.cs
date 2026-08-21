using CampusCore.Application.Abstractions;
using CampusCore.Domain.Entities;
using CampusCore.Domain.Enums;
using CampusCore.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Endpoints;

public static class OperationsEndpoints
{
    public sealed record EnrollmentRequest(Guid StudentId, Guid AcademicYearId, Guid SectionId, DateOnly EnrolledOn, string? RollNumber);
    public sealed record LeaveCreateRequest(Guid StudentId, DateOnly StartsOn, DateOnly EndsOn, string Reason);
    public sealed record LeaveDecisionRequest(LeaveStatus Status, string? Note);
    public sealed record StaffRequest(string EmployeeNumber, string FirstName, string LastName, string Email, string? Phone, string JobTitle);
    public sealed record TimetableRequest(Guid SectionId, Guid SubjectId, Guid? StaffMemberId, DayOfWeek DayOfWeek, TimeOnly StartsAt, TimeOnly EndsAt, string? Room);

    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/operations").WithTags("Operations").RequireAuthorization();

        group.MapPost("/enrollments", async (EnrollmentRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (await db.Enrollments.AnyAsync(x => x.StudentId == request.StudentId && x.AcademicYearId == request.AcademicYearId, ct))
                return Results.Conflict(new { message = "Student already has an enrollment for this academic year." });
            if (!await db.Students.AnyAsync(x => x.Id == request.StudentId, ct) || !await db.Sections.AnyAsync(x => x.Id == request.SectionId, ct) || !await db.AcademicYears.AnyAsync(x => x.Id == request.AcademicYearId, ct))
                return Results.BadRequest(new { message = "Student, section, or academic year is invalid." });
            var entity = new Enrollment { StudentId = request.StudentId, AcademicYearId = request.AcademicYearId, SectionId = request.SectionId, EnrolledOn = request.EnrolledOn, RollNumber = string.IsNullOrWhiteSpace(request.RollNumber) ? null : request.RollNumber.Trim() };
            db.Enrollments.Add(entity); await db.SaveChangesAsync(ct);
            await audit.WriteAsync("enrollment.created", nameof(Enrollment), entity.Id.ToString(), new { request.StudentId, request.AcademicYearId, request.SectionId }, ct);
            return Results.Created($"/api/operations/enrollments/{entity.Id}", new { entity.Id });
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));

        group.MapPost("/leave", async (LeaveCreateRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (request.EndsOn < request.StartsOn || string.IsNullOrWhiteSpace(request.Reason)) return Results.BadRequest(new { message = "Leave dates and reason are invalid." });
            if (!await db.Students.AnyAsync(x => x.Id == request.StudentId, ct)) return Results.NotFound();
            var entity = new LeaveRequest { StudentId = request.StudentId, StartsOn = request.StartsOn, EndsOn = request.EndsOn, Reason = request.Reason.Trim() };
            db.LeaveRequests.Add(entity); await db.SaveChangesAsync(ct); await audit.WriteAsync("leave.created", nameof(LeaveRequest), entity.Id.ToString(), new { request.StudentId, request.StartsOn, request.EndsOn }, ct); return Results.Created($"/api/operations/leave/{entity.Id}", new { entity.Id });
        });
        group.MapPatch("/leave/{id:guid}", async (Guid id, LeaveDecisionRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (request.Status is not (LeaveStatus.Approved or LeaveStatus.Rejected)) return Results.BadRequest(new { message = "Decision must be Approved or Rejected." });
            var entity = await db.LeaveRequests.SingleOrDefaultAsync(x => x.Id == id, ct); if (entity is null) return Results.NotFound();
            entity.Status = request.Status; entity.DecisionNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(); await db.SaveChangesAsync(ct);
            await audit.WriteAsync("leave.decided", nameof(LeaveRequest), id.ToString(), new { request.Status }, ct); return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));

        group.MapGet("/staff", async (IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.StaffMembers.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(ct)));
        group.MapPost("/staff", async (StaffRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.EmployeeNumber) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.Email)) return Results.BadRequest();
            if (await db.StaffMembers.AnyAsync(x => x.EmployeeNumber == request.EmployeeNumber || x.Email == request.Email, ct)) return Results.Conflict();
            var entity = new StaffMember { EmployeeNumber = request.EmployeeNumber.Trim(), FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = request.Email.Trim(), Phone = request.Phone?.Trim(), JobTitle = request.JobTitle.Trim() };
            db.StaffMembers.Add(entity); await db.SaveChangesAsync(ct); await audit.WriteAsync("staff.created", nameof(StaffMember), entity.Id.ToString(), new { entity.EmployeeNumber, entity.JobTitle }, ct); return Results.Created($"/api/operations/staff/{entity.Id}", new { entity.Id });
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator));

        group.MapGet("/timetable/{sectionId:guid}", async (Guid sectionId, IApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.TimetableEntries.AsNoTracking().Where(x => x.SectionId == sectionId).Include(x => x.Subject).Include(x => x.StaffMember).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartsAt).ToListAsync(ct)));
        group.MapPost("/timetable", async (TimetableRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (request.EndsAt <= request.StartsAt) return Results.BadRequest(new { message = "End time must be after start time." });
            var overlap = await db.TimetableEntries.AnyAsync(x => x.SectionId == request.SectionId && x.DayOfWeek == request.DayOfWeek && x.StartsAt < request.EndsAt && x.EndsAt > request.StartsAt, ct);
            if (overlap) return Results.Conflict(new { message = "Timetable entry overlaps an existing period." });
            var entity = new TimetableEntry { SectionId = request.SectionId, SubjectId = request.SubjectId, StaffMemberId = request.StaffMemberId, DayOfWeek = request.DayOfWeek, StartsAt = request.StartsAt, EndsAt = request.EndsAt, Room = request.Room?.Trim() };
            db.TimetableEntries.Add(entity); await db.SaveChangesAsync(ct); await audit.WriteAsync("timetable.created", nameof(TimetableEntry), entity.Id.ToString(), new { request.SectionId, request.SubjectId, request.DayOfWeek, request.StartsAt, request.EndsAt }, ct); return Results.Created($"/api/operations/timetable/{entity.Id}", new { entity.Id });
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));
        return endpoints;
    }
}
