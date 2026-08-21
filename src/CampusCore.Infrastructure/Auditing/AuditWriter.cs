using System.Text.Json;
using CampusCore.Application.Abstractions;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace CampusCore.Infrastructure.Auditing;

public sealed class AuditWriter(ApplicationDbContext db, ICurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(string action, string entityType, string entityId, object? safeMetadata = null, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            ActorUserId = currentUser.IsAuthenticated ? currentUser.UserId : "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            SafeMetadataJson = safeMetadata is null ? null : JsonSerializer.Serialize(safeMetadata, JsonOptions),
            CorrelationId = httpContextAccessor.HttpContext?.TraceIdentifier
        };
        db.AuditLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
