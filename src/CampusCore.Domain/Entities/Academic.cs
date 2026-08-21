using CampusCore.Domain.Common;
using CampusCore.Domain.Enums;

namespace CampusCore.Domain.Entities;

public sealed class Subject : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MaximumMarks { get; set; } = 100m;
}

public sealed class AttendanceRecord : AuditableEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
}

public sealed class LeaveRequest : AuditableEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? DecisionNote { get; set; }
}

public sealed class Mark : AuditableEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public Guid AcademicYearId { get; set; }
    public AcademicYear? AcademicYear { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaximumScore { get; set; } = 100m;

    public decimal Percentage => MaximumScore <= 0 ? 0 : Math.Round(Score / MaximumScore * 100m, 2);
}

public sealed class GradeScale : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal MinimumPercentage { get; set; }
    public decimal MaximumPercentage { get; set; } = 100m;
    public string Grade { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class TimetableEntry : AuditableEntity
{
    public Guid SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public Guid? StaffMemberId { get; set; }
    public StaffMember? StaffMember { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public string? Room { get; set; }
}
