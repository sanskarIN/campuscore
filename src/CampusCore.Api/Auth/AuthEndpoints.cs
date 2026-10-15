using System.Security.Cryptography;
using System.Text;
using CampusCore.Api.Validation;
using CampusCore.Application.Abstractions;
using CampusCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Auth;

public static class AuthEndpoints
{
    private static readonly SemaphoreSlim BootstrapGate = new(1, 1);

    public sealed record LoginRequest(string Email, string Password);
    public sealed record BootstrapRequest(string Email, string Password, string DisplayName);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest request, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, TokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 || string.IsNullOrEmpty(request.Password) || request.Password.Length > 256)
                return Results.Unauthorized();

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await users.FindByEmailAsync(email);
            if (user is null || !user.IsActive) return Results.Unauthorized();

            var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded) return Results.Unauthorized();
            return Results.Ok(await tokens.CreateAsync(user));
        }).RequireRateLimiting("auth");

        group.MapPost("/bootstrap", async (
            HttpRequest http,
            BootstrapRequest request,
            IConfiguration config,
            UserManager<ApplicationUser> users,
            TokenService tokens,
            IAuditWriter audit,
            CancellationToken ct) =>
        {
            var configured = config["BootstrapAdmin:Key"];
            var supplied = http.Headers["X-Bootstrap-Key"].ToString();
            if (!BootstrapKeyMatches(configured, supplied)) return Results.Unauthorized();

            string email;
            string displayName;
            try
            {
                email = RequestText.Required(request.Email, "Email", 254).ToLowerInvariant();
                displayName = RequestText.Required(request.DisplayName, "Display name", 200);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }

            if (string.IsNullOrEmpty(request.Password) || request.Password.Length > 256)
                return Results.BadRequest(new { message = "Password is required and cannot exceed 256 characters." });

            await BootstrapGate.WaitAsync(ct);
            try
            {
                if (await users.Users.AnyAsync(ct))
                    return Results.Conflict(new { message = "Bootstrap is disabled after the first account is created." });

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    DisplayName = displayName,
                    EmailConfirmed = true,
                    IsActive = true
                };
                var created = await users.CreateAsync(user, request.Password);
                if (!created.Succeeded) return Results.ValidationProblem(ToValidationErrors(created));

                var role = await users.AddToRoleAsync(user, CampusRoles.Administrator);
                if (!role.Succeeded)
                {
                    var rollback = await users.DeleteAsync(user);
                    if (!rollback.Succeeded)
                        return Results.Problem("Administrator bootstrap failed and cleanup could not be completed.");
                    return Results.Problem("Administrator role could not be assigned.");
                }

                await audit.WriteAsync("user.bootstrap_created", nameof(ApplicationUser), user.Id, new { Role = CampusRoles.Administrator }, ct);
                return Results.Ok(await tokens.CreateAsync(user));
            }
            finally
            {
                BootstrapGate.Release();
            }
        }).RequireRateLimiting("auth");

        group.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) => Results.Ok(new
        {
            id = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            name = user.Identity?.Name,
            roles = user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x => x.Value).ToArray()
        })).RequireAuthorization();

        return endpoints;
    }

    private static bool BootstrapKeyMatches(string? configured, string supplied)
    {
        if (string.IsNullOrWhiteSpace(configured) || string.IsNullOrWhiteSpace(supplied)) return false;
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(configuredHash, suppliedHash);
    }

    private static Dictionary<string, string[]> ToValidationErrors(IdentityResult result) =>
        result.Errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(error => error.Description).ToArray());
}
