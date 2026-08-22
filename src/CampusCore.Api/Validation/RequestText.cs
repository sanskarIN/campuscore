namespace CampusCore.Api.Validation;

internal static class RequestText
{
    public static string Required(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{fieldName} is required.");
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.");
        return normalized;
    }

    public static string? Optional(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.");
        return normalized;
    }
}
