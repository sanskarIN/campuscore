using CampusCore.Application.Abstractions;
using CampusCore.Domain.Entities;
using CampusCore.Domain.Enums;
using CampusCore.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Endpoints;

public static class CommunicationEndpoints
{
    public sealed record AnnouncementRequest(string Title, string Body, AnnouncementAudience Audience, DateTimeOffset PublishAtUtc, DateTimeOffset? ExpiresAtUtc);

    public static IEndpointRouteBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/announcements").WithTags("Announcements").RequireAuthorization();
        group.MapGet("/", async (IApplicationDbContext db, CancellationToken ct) =>
        {
            var now = DateTimeOffset.UtcNow;
            return Results.Ok(await db.Announcements.AsNoTracking().Include(x => x.Attachments).Where(x => x.PublishAtUtc <= now && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now)).OrderByDescending(x => x.PublishAtUtc).Take(100).ToListAsync(ct));
        });
        group.MapPost("/", async (AnnouncementRequest request, IApplicationDbContext db, IAuditWriter audit, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body)) return Results.BadRequest();
            if (request.ExpiresAtUtc is not null && request.ExpiresAtUtc <= request.PublishAtUtc) return Results.BadRequest(new { message = "Expiry must be after publish time." });
            var x = new Announcement { Title = request.Title.Trim(), Body = request.Body.Trim(), Audience = request.Audience, PublishAtUtc = request.PublishAtUtc, ExpiresAtUtc = request.ExpiresAtUtc };
            db.Announcements.Add(x); await db.SaveChangesAsync(ct); await audit.WriteAsync("announcement.created", nameof(Announcement), x.Id.ToString(), new { x.Audience, x.PublishAtUtc }, ct);
            return Results.Created($"/api/announcements/{x.Id}", new { x.Id });
        }).RequireAuthorization(p => p.RequireRole(CampusRoles.Administrator, CampusRoles.Registrar));
        return endpoints;
    }
}
