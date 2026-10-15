namespace CampusCore.Api.Configuration;

public static class ProductionConfigurationValidator
{
    private static readonly string[] UnsafeMarkers =
    [
        "development-only",
        "local-only",
        "change-before-production",
        "replace-with"
    ];

    public static void Validate(IConfiguration configuration, bool isProduction)
    {
        if (!isProduction) return;

        var jwtKey = configuration["Jwt:Key"];
        RequireSecret("Jwt:Key", jwtKey, minimumLength: 32);

        var database = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(database))
            throw new InvalidOperationException("ConnectionStrings:Database must be configured in production.");
        RejectUnsafeMarkers("ConnectionStrings:Database", database);

        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
            throw new InvalidOperationException("AllowedHosts must explicitly list production host names.");

        ValidateCorsOrigins(configuration.GetSection("Cors:Origins").Get<string[]>() ?? []);

        var bootstrapKey = configuration["BootstrapAdmin:Key"];
        if (!string.IsNullOrWhiteSpace(bootstrapKey))
            RequireSecret("BootstrapAdmin:Key", bootstrapKey, minimumLength: 24);
    }

    private static void ValidateCorsOrigins(IEnumerable<string> origins)
    {
        foreach (var rawOrigin in origins)
        {
            var origin = rawOrigin?.Trim();
            if (string.IsNullOrWhiteSpace(origin)) continue;

            RejectUnsafeMarkers("Cors:Origins", origin);

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment) ||
                uri.AbsolutePath != "/")
            {
                throw new InvalidOperationException("Cors:Origins entries must be HTTPS origins containing only scheme, host, and optional port in production.");
            }
        }
    }

    private static void RequireSecret(string name, string? value, int minimumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < minimumLength)
            throw new InvalidOperationException($"{name} must contain at least {minimumLength} characters in production.");

        RejectUnsafeMarkers(name, value);
    }

    private static void RejectUnsafeMarkers(string name, string value)
    {
        if (UnsafeMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"{name} contains a known development placeholder and cannot be used in production.");
    }
}
