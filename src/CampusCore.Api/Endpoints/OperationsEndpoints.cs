using CampusCore.Api.Validation;
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

            var studentExists = await db.Students.AnyAsync(x => x.Id == request.StudentId, ct);
            var sectionExists = await db.Sections.AnyAsync(x => x.Id == request.SectionId, ct);
            var academicYear = await db.AcademicYears.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.AcademicYearId, ct);
            if (!studentExists || !sectionExists || academicYear is null)
                return Results.BadRequest(new { message = "Student, section, or academic year is invalid." });
            if (request.EnrolledOn < academicYear.StartsOn || request.EnrolledOn > academicYear.EndsOn)
                return Results.BadRequest(new { message = "Enrollment date must fall within the selected academic year." });

            var entity = new Enrollment
            {
                StudentId = request.StudentId,
                AcademicYearId = request.AcademicYearId,
                SectionId = request.SectionId,
                EnrolledOn = request.EnrolledOn,
                RollNumber = RequestText.Optional(request.RollNumber, "Roll number", 64)
            };
            db.Enrollments.Add(entity);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("enrollment.created", nameof(Enrollment), entity.Id.ToString(), new { request.StudentId, request.AcademicYearId, request.SectionId }, ct);
            return Results.Created($"/api/operations/enrollments/{entity.Id}", new { entity.Id });
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));

        group.MapPost("/leave", async (LeaveCreateRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (request.EndsOn < request.StartsOn) return Results.BadRequest(new { message = "Leave end date cannot be before its start date." });
            var reason = RequestText.Required(request.Reason, "Leave reason", 1000);
            if (!await db.Students.AnyAsync(x => x.Id == request.StudentId, ct)) return Results.NotFound();
            var entity = new LeaveRequest { StudentId = request.StudentId, StartsOn = request.StartsOn, EndsOn = request.EndsOn, Reason = reason };
            db.LeaveRequests.Add(entity);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("leave.created", nameof(LeaveRequest), entity.Id.ToString(), new { request.StudentId, request.StartsOn, request.EndsOn }, ct);
            return Results.Created($"/api/operations/leave/{entity.Id}", new { entity.Id });
        });

        group.MapPatch("/leave/{id:guid}", async (Guid id, LeaveDecisionRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (request.Status is not (LeaveStatus.Approved or LeaveStatus.Rejected)) return Results.BadRequest(new { message = "Decision must be Approved or Rejected." });
            var entity = await db.LeaveRequests.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return Results.NotFound();
            entity.Status = request.Status;
            entity.DecisionNote = RequestText.Optional(request.Note, "Decision note", 500);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("leave.decided", nameof(LeaveRequest), id.ToString(), new { request.Status }, ct);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));

        group.MapGet("/staff", async (IApplicationDbContext db, CancellationToken ct) =>
            Results.Ok(await db.StaffMembers.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(ct)));

        group.MapPost("/staff", async (StaffRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            var employeeNumber = RequestText.Required(request.EmployeeNumber, "Employee number", 64);
            var firstName = RequestText.Required(request.FirstName, "First name", 120);
            var lastName = RequestText.Optional(request.LastName, "Last name", 120) ?? string.Empty;
            var email = RequestText.Required(request.Email, "Email", 254).ToLowerInvariant();
            var phone = RequestText.Optional(request.Phone, "Phone", 40);
            var jobTitle = RequestText.Required(request.JobTitle, "Job title", 120);
            if (await db.StaffMembers.AnyAsync(x => x.EmployeeNumber == employeeNumber || x.Email.ToLower() == email, ct))
                return Results.Conflict(new { message = "Employee number or email already exists." });

            var entity = new StaffMember
            {
                EmployeeNumber = employeeNumber,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                JobTitle = jobTitle
            };
            db.StaffMembers.Add(entity);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("staff.created", nameof(StaffMember), entity.Id.ToString(), new { entity.EmployeeNumber, entity.JobTitle }, ct);
            return Results.Created($"/api/operations/staff/{entity.Id}", new { entity.Id });
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator));

        group.MapGet("/timetable/{sectionId:guid}", async (Guid sectionId, IApplicationDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TimetableEntries.AsNoTracking().Where(x => x.SectionId == sectionId).Include(x => x.Subject).Include(x => x.StaffMember).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartsAt).ToListAsync(ct)));

        group.MapPost("/timetable", async (TimetableRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (!Enum.IsDefined(typeof(DayOfWeek), request.DayOfWeek)) return Results.BadRequest(new { message = "Day of week is invalid." });
            if (request.EndsAt <= request.StartsAt) return Results.BadRequest(new { message = "End time must be after start time." });
            if (!await db.Sections.AnyAsync(x => x.Id == request.SectionId, ct) || !await db.Subjects.AnyAsync(x => x.Id == request.SubjectId, ct))
                return Results.BadRequest(new { message = "Section or subject is invalid." });
            if (request.StaffMemberId.HasValue && !await db.StaffMembers.AnyAsync(x => x.Id == request.StaffMemberId.Value, ct))
                return Results.BadRequest(new { message = "Staff member is invalid." });

            var sectionOverlap = await db.TimetableEntries.AnyAsync(x => x.SectionId == request.SectionId && x.DayOfWeek == request.DayOfWeek && x.StartsAt < request.EndsAt && x.EndsAt > request.StartsAt, ct);
            if (sectionOverlap) return Results.Conflict(new { message = "Timetable entry overlaps an existing period for this section." });
            if (request.StaffMemberId.HasValue)
            {
                var staffOverlap = await db.TimetableEntries.AnyAsync(x => x.StaffMemberId == request.StaffMemberId.Value && x.DayOfWeek == request.DayOfWeek && x.StartsAt < request.EndsAt && x.EndsAt > request.StartsAt, ct);
                if (staffOverlap) return Results.Conflict(new { message = "Staff member is already assigned during this period." });
            }

            var entity = new TimetableEntry
            {
                SectionId = request.SectionId,
                SubjectId = request.SubjectId,
                StaffMemberId = request.StaffMemberId,
                DayOfWeek = request.DayOfWeek,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt,
                Room = RequestText.Optional(request.Room, "Room", 80)
            };
            db.TimetableEntries.Add(entity);
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("timetable.created", nameof(TimetableEntry), entity.Id.ToString(), new { request.SectionId, request.SubjectId, request.DayOfWeek, request.StartsAt, request.EndsAt }, ct);
            return Results.Created($"/api/operations/timetable/{entity.Id}", new { entity.Id });
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));
        return endpoints;
    }
}
