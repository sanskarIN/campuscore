using CampusCore.Domain.Common;

namespace CampusCore.Domain.Entities;

public sealed class Student : AuditableEntity
{
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Guardian> Guardians { get; set; } = new List<Guardian>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public string DisplayName => string.Join(' ', new[] { FirstName.Trim(), LastName.Trim() }.Where(x => x.Length > 0));

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AdmissionNumber)) throw new ArgumentException("Admission number is required.");
        if (string.IsNullOrWhiteSpace(FirstName)) throw new ArgumentException("First name is required.");
        if (DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow)) throw new ArgumentException("Date of birth cannot be in the future.");
    }
}

public sealed class Guardian : AuditableEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsPrimary { get; set; }
}
