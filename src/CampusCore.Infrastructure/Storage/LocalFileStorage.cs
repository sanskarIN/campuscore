using CampusCore.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CampusCore.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configured = configuration["Storage:RootPath"];
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "data", "uploads")
            : configured);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var normalizedExtension = NormalizeExtension(extension);
        var storedName = $"{Guid.NewGuid():N}{normalizedExtension}";
        var destination = Resolve(storedName);
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(output, cancellationToken);
        return storedName;
    }

    public Task<Stream?> OpenReadAsync(string storedName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(storedName);
        if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storedName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(storedName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string storedName)
    {
        if (string.IsNullOrWhiteSpace(storedName) ||
            storedName.Contains('/') ||
            storedName.Contains('\\') ||
            Path.GetFileName(storedName) != storedName)
        {
            throw new ArgumentException("Stored file name is invalid.", nameof(storedName));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_root, storedName));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Resolved storage path is outside the configured root.");
        return fullPath;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return string.Empty;
        var normalized = extension.StartsWith('.') ? extension : $".{extension}";
        if (normalized.Length > 12 || normalized.Any(ch => !char.IsLetterOrDigit(ch) && ch != '.'))
            throw new ArgumentException("File extension is invalid.", nameof(extension));
        return normalized.ToLowerInvariant();
    }
}
