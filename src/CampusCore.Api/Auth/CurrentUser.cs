using System.Security.Claims;
using CampusCore.Application.Abstractions;

namespace CampusCore.Api.Auth;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public string UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
