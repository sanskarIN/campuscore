namespace CampusCore.Application.Students;

public sealed record StudentListItem(
    Guid Id,
    string AdmissionNumber,
    string DisplayName,
    DateOnly DateOfBirth,
    bool IsActive,
    string? ClassName,
    string? SectionName,
    string? RollNumber);

public sealed record StudentDetails(
    Guid Id,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Email,
    string? Phone,
    string? AddressLine,
    bool IsActive,
    IReadOnlyList<GuardianModel> Guardians,
    IReadOnlyList<EnrollmentModel> Enrollments);

public sealed record GuardianModel(Guid Id, string Name, string Relationship, string? Email, string? Phone, bool IsPrimary);
public sealed record EnrollmentModel(Guid Id, Guid AcademicYearId, string AcademicYear, Guid SectionId, string ClassName, string SectionName, string? RollNumber, string Status);

public sealed record CreateStudentRequest(
    string AdmissionNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Email,
    string? Phone,
    string? AddressLine);

public sealed record UpdateStudentRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Email,
    string? Phone,
    string? AddressLine,
    bool IsActive);

public sealed record UpsertGuardianRequest(string Name, string Relationship, string? Email, string? Phone, bool IsPrimary);
