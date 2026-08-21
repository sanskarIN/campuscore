using CampusCore.Domain.Common;

namespace CampusCore.Domain.Entities;

public sealed class InstitutionSettings : AuditableEntity
{
    public string InstitutionName { get; set; } = "CampusCore School";
    public string? Address { get; set; }
    public string? TimeZoneId { get; set; } = "UTC";
    public string Locale { get; set; } = "en";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public int DefaultPageSize { get; set; } = 25;
    public bool AllowGuardianPortal { get; set; }
}

public sealed class AuditLog : Entity
{
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string ActorUserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? SafeMetadataJson { get; set; }
    public string? CorrelationId { get; set; }
}
