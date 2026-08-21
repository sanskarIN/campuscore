using CampusCore.Domain.Common;
using CampusCore.Domain.Enums;

namespace CampusCore.Domain.Entities;

public sealed class Announcement : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Everyone;
    public DateTimeOffset PublishAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public ICollection<AnnouncementAttachment> Attachments { get; set; } = new List<AnnouncementAttachment>();
}

public sealed class AnnouncementAttachment : AuditableEntity
{
    public Guid AnnouncementId { get; set; }
    public Announcement? Announcement { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
