using CampusCore.Application.Abstractions;
using CampusCore.Domain.Entities;
using CampusCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Application.Academics;

public sealed record AttendanceUpsert(Guid StudentId, DateOnly Date, AttendanceStatus Status, string? Note);
public sealed record MarkUpsert(Guid StudentId, Guid SubjectId, Guid AcademicYearId, string AssessmentName, decimal Score, decimal MaximumScore);
public sealed record GradeResult(string Grade, string? Description, decimal Percentage);

public sealed class AcademicService(IApplicationDbContext db, IAuditWriter audit)
{
    public async Task UpsertAttendanceAsync(AttendanceUpsert request, CancellationToken cancellationToken)
    {
        var studentExists = await db.Students.AnyAsync(x => x.Id == request.StudentId && x.IsActive, cancellationToken);
        if (!studentExists) throw new ArgumentException("Student does not exist or is inactive.");
        if (request.Date > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))) throw new ArgumentException("Attendance date is invalid.");

        var entity = await db.AttendanceRecords.SingleOrDefaultAsync(x => x.StudentId == request.StudentId && x.Date == request.Date, cancellationToken);
        if (entity is null)
        {
            entity = new AttendanceRecord { StudentId = request.StudentId, Date = request.Date };
            db.AttendanceRecords.Add(entity);
        }

        entity.Status = request.Status;
        entity.Note = NormalizeOptional(request.Note);
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("attendance.upserted", nameof(AttendanceRecord), entity.Id.ToString(), new { request.StudentId, request.Date, request.Status }, cancellationToken);
    }

    public async Task<Guid> RecordMarkAsync(MarkUpsert request, CancellationToken cancellationToken)
    {
        if (request.MaximumScore <= 0 || request.Score < 0 || request.Score > request.MaximumScore)
            throw new ArgumentException("Score must be between zero and maximum score.");
        if (string.IsNullOrWhiteSpace(request.AssessmentName)) throw new ArgumentException("Assessment name is required.");

        var studentExists = await db.Students.AnyAsync(x => x.Id == request.StudentId, cancellationToken);
        var subjectExists = await db.Subjects.AnyAsync(x => x.Id == request.SubjectId, cancellationToken);
        var yearExists = await db.AcademicYears.AnyAsync(x => x.Id == request.AcademicYearId, cancellationToken);
        if (!studentExists || !subjectExists || !yearExists) throw new ArgumentException("Student, subject, or academic year is invalid.");

        var mark = new Mark
        {
            StudentId = request.StudentId,
            SubjectId = request.SubjectId,
            AcademicYearId = request.AcademicYearId,
            AssessmentName = request.AssessmentName.Trim(),
            Score = request.Score,
            MaximumScore = request.MaximumScore
        };
        db.Marks.Add(mark);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("mark.recorded", nameof(Mark), mark.Id.ToString(), new { request.StudentId, request.SubjectId, request.AssessmentName }, cancellationToken);
        return mark.Id;
    }

    public async Task<GradeResult?> ResolveGradeAsync(decimal percentage, CancellationToken cancellationToken)
    {
        if (percentage is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percentage));
        var grade = await db.GradeScales.AsNoTracking()
            .Where(x => percentage >= x.MinimumPercentage && percentage <= x.MaximumPercentage)
            .OrderByDescending(x => x.MinimumPercentage)
            .FirstOrDefaultAsync(cancellationToken);
        return grade is null ? null : new GradeResult(grade.Grade, grade.Description, percentage);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
