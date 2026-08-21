using Microsoft.AspNetCore.Identity;

namespace CampusCore.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public static class CampusRoles
{
    public const string Administrator = "Administrator";
    public const string Registrar = "Registrar";
    public const string Teacher = "Teacher";
    public const string Viewer = "Viewer";
    public static readonly string[] All = [Administrator, Registrar, Teacher, Viewer];
}
