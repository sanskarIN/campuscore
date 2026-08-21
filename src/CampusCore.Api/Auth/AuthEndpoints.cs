using CampusCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusCore.Api.Auth;

public static class AuthEndpoints
{
    public sealed record LoginRequest(string Email, string Password);
    public sealed record BootstrapRequest(string Email, string Password, string DisplayName);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest request, UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, TokenService tokens) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await users.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == email.ToUpperInvariant());
            if (user is null || !user.IsActive) return Results.Unauthorized();
            var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded) return Results.Unauthorized();
            return Results.Ok(await tokens.CreateAsync(user));
        }).RequireRateLimiting("auth");

        group.MapPost("/bootstrap", async (HttpRequest http, BootstrapRequest request, IConfiguration config, UserManager<ApplicationUser> users, TokenService tokens) =>
        {
            var configured = config["BootstrapAdmin:Key"];
            var supplied = http.Headers["X-Bootstrap-Key"].ToString();
            if (string.IsNullOrWhiteSpace(configured) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(configured), System.Text.Encoding.UTF8.GetBytes(supplied)))
                return Results.Unauthorized();
            if (await users.Users.AnyAsync()) return Results.Conflict(new { message = "Bootstrap is disabled after the first account is created." });
            if (string.IsNullOrWhiteSpace(request.DisplayName)) return Results.BadRequest(new { message = "Display name is required." });

            var user = new ApplicationUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), DisplayName = request.DisplayName.Trim(), EmailConfirmed = true };
            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded) return Results.ValidationProblem(created.Errors.GroupBy(x => x.Code).ToDictionary(g => g.Key, g => g.Select(x => x.Description).ToArray()));
            var role = await users.AddToRoleAsync(user, CampusRoles.Administrator);
            if (!role.Succeeded) return Results.Problem("Administrator role could not be assigned.");
            return Results.Ok(await tokens.CreateAsync(user));
        }).RequireRateLimiting("auth");

        group.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) => Results.Ok(new
        {
            id = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            name = user.Identity?.Name,
            roles = user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x => x.Value).ToArray()
        })).RequireAuthorization();

        return endpoints;
    }
}
