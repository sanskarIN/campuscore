using CampusCore.Api.Validation;
using CampusCore.Application.Abstractions;
using CampusCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Endpoints;

public static class UserAdminEndpoints
{
    public sealed record CreateUserRequest(string Email, string DisplayName, string Password, IReadOnlyList<string> Roles);
    public sealed record UpdateRolesRequest(IReadOnlyList<string> Roles);
    public sealed record UpdateUserStatusRequest(bool IsActive);

    public static IEndpointRouteBuilder MapUserAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/users").WithTags("User administration")
            .RequireAuthorization(policy => policy.RequireRole(CampusRoles.Administrator));

        group.MapGet("/", async (UserManager<ApplicationUser> users, CancellationToken ct) =>
        {
            var items = await users.Users.AsNoTracking().OrderBy(x => x.Email).Select(x => new { x.Id, x.Email, x.DisplayName, x.IsActive, x.LockoutEnd }).ToListAsync(ct);
            var result = new List<object>(items.Count);
            foreach (var item in items)
            {
                var user = await users.FindByIdAsync(item.Id);
                result.Add(new { item.Id, item.Email, item.DisplayName, item.IsActive, item.LockoutEnd, Roles = user is null ? Array.Empty<string>() : await users.GetRolesAsync(user) });
            }
            return Results.Ok(result);
        });

        group.MapPost("/", async (CreateUserRequest request, UserManager<ApplicationUser> users, IAuditWriter audit, CancellationToken ct) =>
        {
            var email = RequestText.Required(request.Email, "Email", 254).ToLowerInvariant();
            var displayName = RequestText.Required(request.DisplayName, "Display name", 200);
            if (string.IsNullOrEmpty(request.Password) || request.Password.Length > 256)
                return Results.BadRequest(new { message = "Password is required and cannot exceed 256 characters." });
            if (request.Roles is null || request.Roles.Count > CampusRoles.All.Length)
                return Results.BadRequest(new { message = "Roles are invalid." });
            if (!TryNormalizeRoles(request.Roles, out var roles))
                return Results.BadRequest(new { message = "One or more roles are invalid." });

            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = displayName, IsActive = true };
            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded) return Results.ValidationProblem(ToValidationErrors(created));
            if (roles.Length > 0)
            {
                var added = await users.AddToRolesAsync(user, roles);
                if (!added.Succeeded)
                {
                    await users.DeleteAsync(user);
                    return Results.ValidationProblem(ToValidationErrors(added));
                }
            }
            await audit.WriteAsync("user.created", nameof(ApplicationUser), user.Id, new { Roles = roles }, ct);
            return Results.Created($"/api/admin/users/{user.Id}", new { user.Id, user.Email, user.DisplayName, Roles = roles });
        });

        group.MapPut("/{id}/roles", async (string id, UpdateRolesRequest request, UserManager<ApplicationUser> users, ICurrentUser currentUser, IAuditWriter audit, CancellationToken ct) =>
        {
            var user = await users.FindByIdAsync(id);
            if (user is null) return Results.NotFound();
            if (request.Roles is null || request.Roles.Count > CampusRoles.All.Length || !TryNormalizeRoles(request.Roles, out var desired))
                return Results.BadRequest(new { message = "One or more roles are invalid." });
            if (id == currentUser.UserId && !desired.Contains(CampusRoles.Administrator, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "You cannot remove your own Administrator role." });

            var current = await users.GetRolesAsync(user);
            var remove = current.Except(desired, StringComparer.OrdinalIgnoreCase).ToArray();
            var add = desired.Except(current, StringComparer.OrdinalIgnoreCase).ToArray();
            if (remove.Length > 0)
            {
                var result = await users.RemoveFromRolesAsync(user, remove);
                if (!result.Succeeded) return Results.ValidationProblem(ToValidationErrors(result));
            }
            if (add.Length > 0)
            {
                var result = await users.AddToRolesAsync(user, add);
                if (!result.Succeeded) return Results.ValidationProblem(ToValidationErrors(result));
            }
            await audit.WriteAsync("user.roles_updated", nameof(ApplicationUser), user.Id, new { Roles = desired }, ct);
            return Results.NoContent();
        });

        group.MapPatch("/{id}/status", async (string id, UpdateUserStatusRequest request, UserManager<ApplicationUser> users, ICurrentUser currentUser, IAuditWriter audit, CancellationToken ct) =>
        {
            var user = await users.FindByIdAsync(id);
            if (user is null) return Results.NotFound();
            if (id == currentUser.UserId && !request.IsActive) return Results.BadRequest(new { message = "You cannot deactivate your own account." });
            user.IsActive = request.IsActive;
            var result = await users.UpdateAsync(user);
            if (!result.Succeeded) return Results.ValidationProblem(ToValidationErrors(result));
            await audit.WriteAsync("user.status_updated", nameof(ApplicationUser), user.Id, new { user.IsActive }, ct);
            return Results.NoContent();
        });

        return endpoints;
    }

    private static bool TryNormalizeRoles(IReadOnlyList<string> requested, out string[] roles)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in requested)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                roles = [];
                return false;
            }
            var canonical = CampusRoles.All.FirstOrDefault(allowed => string.Equals(allowed, role.Trim(), StringComparison.OrdinalIgnoreCase));
            if (canonical is null)
            {
                roles = [];
                return false;
            }
            normalized.Add(canonical);
        }
        roles = normalized.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return true;
    }

    private static Dictionary<string, string[]> ToValidationErrors(IdentityResult result) =>
        result.Errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(e => e.Description).ToArray());
}
