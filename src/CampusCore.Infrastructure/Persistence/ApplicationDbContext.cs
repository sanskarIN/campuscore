using CampusCore.Application.Abstractions;
using CampusCore.Domain.Common;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Mark> Marks => Set<Mark>();
    public DbSet<GradeScale> GradeScales => Set<GradeScale>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementAttachment> AnnouncementAttachments => Set<AnnouncementAttachment>();
    public DbSet<InstitutionSettings> InstitutionSettings => Set<InstitutionSettings>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("campuscore");
        ConfigureStudents(builder);
        ConfigureAcademics(builder);
        ConfigureCommunications(builder);
        ConfigureSystem(builder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAtUtc = now;
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.UpdatedAtUtc = now;
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureStudents(ModelBuilder builder)
    {
        builder.Entity<Student>(e =>
        {
            e.HasIndex(x => x.AdmissionNumber).IsUnique();
            e.Property(x => x.AdmissionNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.Phone).HasMaxLength(40);
        });
        builder.Entity<Guardian>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Relationship).HasMaxLength(80).IsRequired();
            e.HasOne(x => x.Student).WithMany(x => x.Guardians).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Enrollment>(e =>
        {
            e.HasIndex(x => new { x.StudentId, x.AcademicYearId }).IsUnique();
            e.HasOne(x => x.Student).WithMany(x => x.Enrollments).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AcademicYear).WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Section>(e => e.HasOne(x => x.SchoolClass).WithMany(x => x.Sections).HasForeignKey(x => x.SchoolClassId).OnDelete(DeleteBehavior.Restrict));
    }

    private static void ConfigureAcademics(ModelBuilder builder)
    {
        builder.Entity<AcademicYear>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<SchoolClass>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<Subject>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<StaffMember>().HasIndex(x => x.EmployeeNumber).IsUnique();
        builder.Entity<StaffMember>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<AttendanceRecord>().HasIndex(x => new { x.StudentId, x.Date }).IsUnique();
        builder.Entity<Mark>().Property(x => x.Score).HasPrecision(8, 2);
        builder.Entity<Mark>().Property(x => x.MaximumScore).HasPrecision(8, 2);
        builder.Entity<GradeScale>().Property(x => x.MinimumPercentage).HasPrecision(5, 2);
        builder.Entity<GradeScale>().Property(x => x.MaximumPercentage).HasPrecision(5, 2);
        builder.Entity<TimetableEntry>().HasIndex(x => new { x.SectionId, x.DayOfWeek, x.StartsAt });
    }

    private static void ConfigureCommunications(ModelBuilder builder)
    {
        builder.Entity<Announcement>(e => e.HasMany(x => x.Attachments).WithOne(x => x.Announcement).HasForeignKey(x => x.AnnouncementId).OnDelete(DeleteBehavior.Cascade));
        builder.Entity<AnnouncementAttachment>().Property(x => x.StoredName).HasMaxLength(200).IsRequired();
    }

    private static void ConfigureSystem(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => x.OccurredAtUtc);
            e.Property(x => x.ActorUserId).HasMaxLength(450);
            e.Property(x => x.Action).HasMaxLength(120).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(120).IsRequired();
        });
    }
}
