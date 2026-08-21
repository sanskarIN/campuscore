using CampusCore.Application.Abstractions;
using CampusCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Application.Reports;

public sealed record ReportCardSubject(
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    decimal Earned,
    decimal Maximum,
    decimal Percentage,
    string? Grade,
    IReadOnlyList<ReportCardAssessment> Assessments);

public sealed record ReportCardAssessment(string Name, decimal Score, decimal MaximumScore, decimal Percentage);

public sealed record ReportCard(
    Guid StudentId,
    string AdmissionNumber,
    string StudentName,
    Guid AcademicYearId,
    string AcademicYear,
    string? ClassName,
    string? SectionName,
    string? RollNumber,
    decimal OverallPercentage,
    string? OverallGrade,
    IReadOnlyList<ReportCardSubject> Subjects,
    DateTimeOffset GeneratedAtUtc);

public sealed class ReportCardService(IApplicationDbContext db)
{
    public async Task<ReportCard?> GetAsync(Guid studentId, Guid academicYearId, CancellationToken cancellationToken)
    {
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId, cancellationToken);
        if (student is null) return null;

        var year = await db.AcademicYears.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academicYearId, cancellationToken);
        if (year is null) return null;

        var enrollment = await db.Enrollments.AsNoTracking()
            .Include(x => x.Section).ThenInclude(x => x!.SchoolClass)
            .SingleOrDefaultAsync(x => x.StudentId == studentId && x.AcademicYearId == academicYearId, cancellationToken);

        var marks = await db.Marks.AsNoTracking()
            .Include(x => x.Subject)
            .Where(x => x.StudentId == studentId && x.AcademicYearId == academicYearId)
            .OrderBy(x => x.Subject!.Name).ThenBy(x => x.AssessmentName)
            .ToListAsync(cancellationToken);
        var grades = await db.GradeScales.AsNoTracking().OrderByDescending(x => x.MinimumPercentage).ToListAsync(cancellationToken);

        var subjectRows = marks
            .GroupBy(x => new { x.SubjectId, Code = x.Subject!.Code, Name = x.Subject.Name })
            .Select(group =>
            {
                var earned = group.Sum(x => x.Score);
                var maximum = group.Sum(x => x.MaximumScore);
                var percentage = Percentage(earned, maximum);
                return new ReportCardSubject(
                    group.Key.SubjectId,
                    group.Key.Code,
                    group.Key.Name,
                    earned,
                    maximum,
                    percentage,
                    FindGrade(grades, percentage)?.Grade,
                    group.Select(x => new ReportCardAssessment(x.AssessmentName, x.Score, x.MaximumScore, x.Percentage)).ToList());
            })
            .OrderBy(x => x.SubjectName)
            .ToList();

        var totalEarned = subjectRows.Sum(x => x.Earned);
        var totalMaximum = subjectRows.Sum(x => x.Maximum);
        var overall = Percentage(totalEarned, totalMaximum);

        return new ReportCard(
            student.Id,
            student.AdmissionNumber,
            student.DisplayName,
            year.Id,
            year.Name,
            enrollment?.Section?.SchoolClass?.Name,
            enrollment?.Section?.Name,
            enrollment?.RollNumber,
            overall,
            FindGrade(grades, overall)?.Grade,
            subjectRows,
            DateTimeOffset.UtcNow);
    }

    private static decimal Percentage(decimal earned, decimal maximum) => maximum <= 0m ? 0m : Math.Round(earned / maximum * 100m, 2);

    private static GradeScale? FindGrade(IEnumerable<GradeScale> grades, decimal percentage) =>
        grades.FirstOrDefault(x => percentage >= x.MinimumPercentage && percentage <= x.MaximumPercentage);
}
