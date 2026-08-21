namespace CampusCore.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(string action, string entityType, string entityId, object? safeMetadata = null, CancellationToken cancellationToken = default);
}
