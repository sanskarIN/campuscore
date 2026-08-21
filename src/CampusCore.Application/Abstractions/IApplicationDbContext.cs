using CampusCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Student> Students { get; }
    DbSet<Guardian> Guardians { get; }
    DbSet<AcademicYear> AcademicYears { get; }
    DbSet<SchoolClass> SchoolClasses { get; }
    DbSet<Section> Sections { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<StaffMember> StaffMembers { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<Mark> Marks { get; }
    DbSet<GradeScale> GradeScales { get; }
    DbSet<TimetableEntry> TimetableEntries { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<AnnouncementAttachment> AnnouncementAttachments { get; }
    DbSet<InstitutionSettings> InstitutionSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
