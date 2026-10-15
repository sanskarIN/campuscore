using CampusCore.Application.Abstractions;
using CampusCore.Application.Common;
using CampusCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Application.Students;

public sealed class StudentService(IApplicationDbContext db, IAuditWriter audit)
{
    public async Task<PagedResult<StudentListItem>> SearchAsync(string? query, Guid? sectionId, bool? active, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var source = db.Students.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            if (q.Length > 120) throw new ArgumentException("Search query cannot exceed 120 characters.");
            var normalized = q.ToLower();
            source = source.Where(x =>
                x.AdmissionNumber.ToLower().Contains(normalized) ||
                x.FirstName.ToLower().Contains(normalized) ||
                x.LastName.ToLower().Contains(normalized));
        }

        if (active.HasValue) source = source.Where(x => x.IsActive == active.Value);
        if (sectionId.HasValue) source = source.Where(x => x.Enrollments.Any(e => e.SectionId == sectionId.Value && e.Status == Domain.Enums.EnrollmentStatus.Active));

        var total = await source.CountAsync(cancellationToken);
        var items = await source
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new StudentListItem(
                x.Id,
                x.AdmissionNumber,
                (x.FirstName + " " + x.LastName).Trim(),
                x.DateOfBirth,
                x.IsActive,
                x.Enrollments.Where(e => e.Status == Domain.Enums.EnrollmentStatus.Active).Select(e => e.Section!.SchoolClass!.Name).FirstOrDefault(),
                x.Enrollments.Where(e => e.Status == Domain.Enums.EnrollmentStatus.Active).Select(e => e.Section!.Name).FirstOrDefault(),
                x.Enrollments.Where(e => e.Status == Domain.Enums.EnrollmentStatus.Active).Select(e => e.RollNumber).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<StudentListItem>(items, page, pageSize, total);
    }

    public async Task<StudentDetails?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Students.AsNoTracking().Where(x => x.Id == id).Select(x => new StudentDetails(
            x.Id,
            x.AdmissionNumber,
            x.FirstName,
            x.LastName,
            x.DateOfBirth,
            x.Email,
            x.Phone,
            x.AddressLine,
            x.IsActive,
            x.Guardians.OrderByDescending(g => g.IsPrimary).ThenBy(g => g.Name).Select(g => new GuardianModel(g.Id, g.Name, g.Relationship, g.Email, g.Phone, g.IsPrimary)).ToList(),
            x.Enrollments.OrderByDescending(e => e.AcademicYear!.StartsOn).Select(e => new EnrollmentModel(e.Id, e.AcademicYearId, e.AcademicYear!.Name, e.SectionId, e.Section!.SchoolClass!.Name, e.Section.Name, e.RollNumber, e.Status.ToString())).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var admission = NormalizeRequired(request.AdmissionNumber, "Admission number");
        if (await db.Students.AnyAsync(x => x.AdmissionNumber == admission, cancellationToken))
            throw new InvalidOperationException("Admission number already exists.");

        var student = new Student
        {
            AdmissionNumber = admission,
            FirstName = NormalizeRequired(request.FirstName, "First name"),
            LastName = NormalizeOptional(request.LastName) ?? string.Empty,
            DateOfBirth = request.DateOfBirth,
            Email = NormalizeOptional(request.Email),
            Phone = NormalizeOptional(request.Phone),
            AddressLine = NormalizeOptional(request.AddressLine)
        };
        student.Validate();
        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("student.created", nameof(Student), student.Id.ToString(), new { student.AdmissionNumber }, cancellationToken);
        return student.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateStudentRequest request, CancellationToken cancellationToken)
    {
        var student = await db.Students.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (student is null) return false;

        student.FirstName = NormalizeRequired(request.FirstName, "First name");
        student.LastName = NormalizeOptional(request.LastName) ?? string.Empty;
        student.DateOfBirth = request.DateOfBirth;
        student.Email = NormalizeOptional(request.Email);
        student.Phone = NormalizeOptional(request.Phone);
        student.AddressLine = NormalizeOptional(request.AddressLine);
        student.IsActive = request.IsActive;
        student.UpdatedAtUtc = DateTimeOffset.UtcNow;
        student.Validate();
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("student.updated", nameof(Student), id.ToString(), new { student.IsActive }, cancellationToken);
        return true;
    }

    public async Task<Guid?> AddGuardianAsync(Guid studentId, UpsertGuardianRequest request, CancellationToken cancellationToken)
    {
        var exists = await db.Students.AnyAsync(x => x.Id == studentId, cancellationToken);
        if (!exists) return null;

        var name = NormalizeRequired(request.Name, "Guardian name");
        var relationship = NormalizeRequired(request.Relationship, "Guardian relationship");
        if (name.Length > 200) throw new ArgumentException("Guardian name cannot exceed 200 characters.");
        if (relationship.Length > 80) throw new ArgumentException("Guardian relationship cannot exceed 80 characters.");
        var email = NormalizeOptional(request.Email);
        var phone = NormalizeOptional(request.Phone);
        if ((email?.Length ?? 0) > 254) throw new ArgumentException("Guardian email cannot exceed 254 characters.");
        if ((phone?.Length ?? 0) > 40) throw new ArgumentException("Guardian phone cannot exceed 40 characters.");

        if (request.IsPrimary)
        {
            var current = await db.Guardians.Where(x => x.StudentId == studentId && x.IsPrimary).ToListAsync(cancellationToken);
            foreach (var guardian in current) guardian.IsPrimary = false;
        }

        var entity = new Guardian
        {
            StudentId = studentId,
            Name = name,
            Relationship = relationship,
            Email = email,
            Phone = phone,
            IsPrimary = request.IsPrimary
        };
        db.Guardians.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("guardian.created", nameof(Guardian), entity.Id.ToString(), new { StudentId = studentId }, cancellationToken);
        return entity.Id;
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{fieldName} is required.");
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
