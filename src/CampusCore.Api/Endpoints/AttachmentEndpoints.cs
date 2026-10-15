using CampusCore.Api.Validation;
using CampusCore.Application.Abstractions;
using CampusCore.Domain.Entities;
using CampusCore.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Endpoints;

public static class AttachmentEndpoints
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".png"] = ["image/png"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".txt"] = ["text/plain"],
        [".csv"] = ["text/csv", "application/vnd.ms-excel"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
    };

    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/announcements/{announcementId:guid}/attachments").WithTags("Announcement attachments").RequireAuthorization();

        group.MapPost("/", async (Guid announcementId, IFormFile file, IApplicationDbContext db, IFileStorage storage, IAuditWriter audit, CancellationToken ct) =>
        {
            if (!await db.Announcements.AnyAsync(x => x.Id == announcementId, ct)) return Results.NotFound();
            if (file.Length <= 0 || file.Length > MaxBytes) return Results.BadRequest(new { message = "Attachment must be between 1 byte and 10 MB." });

            var normalizedInputName = (file.FileName ?? string.Empty).Replace('\\', '/');
            var leafName = normalizedInputName[(normalizedInputName.LastIndexOf('/') + 1)..];
            var extension = Path.GetExtension(leafName).ToLowerInvariant();
            var declaredContentType = (file.ContentType ?? string.Empty).Split(';', 2)[0].Trim();
            if (!Allowed.TryGetValue(extension, out var contentTypes) || !contentTypes.Contains(declaredContentType, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Attachment type is not allowed." });
            var safeOriginalName = SanitizeFileName(leafName, extension);

            await using var input = file.OpenReadStream();
            using var buffered = new MemoryStream((int)Math.Min(file.Length, MaxBytes));
            await input.CopyToAsync(buffered, ct);
            if (buffered.Length != file.Length)
                return Results.BadRequest(new { message = "Attachment length does not match the uploaded content." });
            var content = buffered.ToArray();
            if (!AttachmentContentValidator.LooksValid(extension, content))
                return Results.BadRequest(new { message = "Attachment content does not match its declared type." });
            buffered.Position = 0;

            var storedName = await storage.SaveAsync(buffered, extension, ct);
            var entity = new AnnouncementAttachment
            {
                AnnouncementId = announcementId,
                FileName = safeOriginalName,
                StoredName = storedName,
                ContentType = declaredContentType,
                SizeBytes = file.Length
            };
            try
            {
                db.AnnouncementAttachments.Add(entity);
                await db.SaveChangesAsync(ct);
                await audit.WriteAsync("announcement_attachment.created", nameof(AnnouncementAttachment), entity.Id.ToString(), new { announcementId, entity.SizeBytes, Extension = extension }, ct);
            }
            catch
            {
                await storage.DeleteAsync(storedName, ct);
                throw;
            }
            return Results.Created($"/api/announcements/{announcementId}/attachments/{entity.Id}", new { entity.Id, entity.FileName, entity.ContentType, entity.SizeBytes });
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar)).DisableAntiforgery();

        group.MapGet("/{attachmentId:guid}", async (Guid announcementId, Guid attachmentId, IApplicationDbContext db, IFileStorage storage, CancellationToken ct) =>
        {
            var entity = await db.AnnouncementAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId && x.AnnouncementId == announcementId, ct);
            if (entity is null) return Results.NotFound();
            var stream = await storage.OpenReadAsync(entity.StoredName, ct);
            return stream is null ? Results.NotFound() : Results.File(stream, entity.ContentType, entity.FileName, enableRangeProcessing: true);
        });

        group.MapDelete("/{attachmentId:guid}", async (Guid announcementId, Guid attachmentId, IApplicationDbContext db, IFileStorage storage, IAuditWriter audit, CancellationToken ct) =>
        {
            var entity = await db.AnnouncementAttachments.SingleOrDefaultAsync(x => x.Id == attachmentId && x.AnnouncementId == announcementId, ct);
            if (entity is null) return Results.NotFound();
            db.AnnouncementAttachments.Remove(entity);
            await db.SaveChangesAsync(ct);
            await storage.DeleteAsync(entity.StoredName, ct);
            await audit.WriteAsync("announcement_attachment.deleted", nameof(AnnouncementAttachment), attachmentId.ToString(), new { announcementId }, ct);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));

        return endpoints;
    }

    private static string SanitizeFileName(string name, string extension)
    {
        var cleaned = new string(name.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) cleaned = $"attachment{extension}";
        if (cleaned.Length <= 240) return cleaned;

        var preservedExtension = Path.GetExtension(cleaned);
        if (preservedExtension.Length > 20) preservedExtension = extension;
        var stemLength = Math.Max(1, 240 - preservedExtension.Length);
        return cleaned[..stemLength] + preservedExtension;
    }
}
