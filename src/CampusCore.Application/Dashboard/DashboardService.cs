using CampusCore.Application.Abstractions;
using CampusCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Application.Dashboard;

public sealed record DashboardSummary(int ActiveStudents, int ActiveStaff, int Sections, int PresentToday, int AbsentToday, int PendingLeaveRequests, int PublishedAnnouncements);

public sealed class DashboardService(IApplicationDbContext db)
{
    public async Task<DashboardSummary> GetAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return new DashboardSummary(
            await db.Students.CountAsync(x => x.IsActive, cancellationToken),
            await db.StaffMembers.CountAsync(x => x.Status == StaffStatus.Active, cancellationToken),
            await db.Sections.CountAsync(cancellationToken),
            await db.AttendanceRecords.CountAsync(x => x.Date == date && (x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late), cancellationToken),
            await db.AttendanceRecords.CountAsync(x => x.Date == date && x.Status == AttendanceStatus.Absent, cancellationToken),
            await db.LeaveRequests.CountAsync(x => x.Status == LeaveStatus.Pending, cancellationToken),
            await db.Announcements.CountAsync(x => x.PublishAtUtc <= now && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now), cancellationToken));
    }
}
