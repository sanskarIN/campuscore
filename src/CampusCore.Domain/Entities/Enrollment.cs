using CampusCore.Domain.Common;
using CampusCore.Domain.Enums;

namespace CampusCore.Domain.Entities;

public sealed class Enrollment : AuditableEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid AcademicYearId { get; set; }
    public AcademicYear? AcademicYear { get; set; }
    public Guid SectionId { get; set; }
    public Section? Section { get; set; }
    public DateOnly EnrolledOn { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public string? RollNumber { get; set; }
}
