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

    public string DisplayName => string.Join(' ', new[] { FirstName?.Trim() ?? string.Empty, LastName?.Trim() ?? string.Empty }.Where(x => x.Length > 0));

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AdmissionNumber)) throw new ArgumentException("Admission number is required.");
        if (AdmissionNumber.Length > 64) throw new ArgumentException("Admission number cannot exceed 64 characters.");
        if (string.IsNullOrWhiteSpace(FirstName)) throw new ArgumentException("First name is required.");
        if (FirstName.Length > 120) throw new ArgumentException("First name cannot exceed 120 characters.");
        if ((LastName?.Length ?? 0) > 120) throw new ArgumentException("Last name cannot exceed 120 characters.");
        if ((Email?.Length ?? 0) > 254) throw new ArgumentException("Email cannot exceed 254 characters.");
        if ((Phone?.Length ?? 0) > 40) throw new ArgumentException("Phone cannot exceed 40 characters.");
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
