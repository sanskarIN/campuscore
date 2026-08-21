using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusCore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace CampusCore.Api.Auth;

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, string DisplayName, IReadOnlyList<string> Roles);

public sealed class TokenService(IConfiguration configuration, UserManager<ApplicationUser> users)
{
    public async Task<AuthResponse> CreateAsync(ApplicationUser user)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
        if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
        var issuer = configuration["Jwt:Issuer"] ?? "CampusCore";
        var audience = configuration["Jwt:Audience"] ?? "CampusCore.Web";
        var roles = await users.GetRolesAsync(user);
        var expires = DateTimeOffset.UtcNow.AddHours(2);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var token = new JwtSecurityToken(issuer, audience, claims, DateTime.UtcNow, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.DisplayName, roles.ToArray());
    }
}
