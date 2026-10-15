using CampusCore.Application.Abstractions;
using CampusCore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Application.Dashboard;

public sealed record DashboardSummary(int ActiveStudents, int ActiveStaff, int Sections, int PresentToday, int AbsentToday, int PendingLeaveRequests, int PublishedAnnouncements);

public sealed class DashboardService(IApplicationDbContext db)
{
    public async Task<DashboardSummary> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var timeZoneId = await db.InstitutionSettings.AsNoTracking().Select(x => x.TimeZoneId).SingleOrDefaultAsync(cancellationToken) ?? "UTC";
        var date = ResolveLocalDate(now, timeZoneId);
        return await GetAsync(date, now, cancellationToken);
    }

    public Task<DashboardSummary> GetAsync(DateOnly date, CancellationToken cancellationToken) =>
        GetAsync(date, DateTimeOffset.UtcNow, cancellationToken);

    private async Task<DashboardSummary> GetAsync(DateOnly date, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return new DashboardSummary(
            await db.Students.CountAsync(x => x.IsActive, cancellationToken),
            await db.StaffMembers.CountAsync(x => x.Status == StaffStatus.Active, cancellationToken),
            await db.Sections.CountAsync(cancellationToken),
            await db.AttendanceRecords.CountAsync(x => x.Date == date && (x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late), cancellationToken),
            await db.AttendanceRecords.CountAsync(x => x.Date == date && x.Status == AttendanceStatus.Absent, cancellationToken),
            await db.LeaveRequests.CountAsync(x => x.Status == LeaveStatus.Pending, cancellationToken),
            await db.Announcements.CountAsync(x => x.PublishAtUtc <= now && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now), cancellationToken));
    }

    internal static DateOnly ResolveLocalDate(DateTimeOffset utcNow, string timeZoneId)
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId);
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).DateTime);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(utcNow.UtcDateTime);
        }
        catch (InvalidTimeZoneException)
        {
            return DateOnly.FromDateTime(utcNow.UtcDateTime);
        }
    }
}
