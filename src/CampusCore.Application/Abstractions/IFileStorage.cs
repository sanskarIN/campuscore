namespace CampusCore.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storedName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storedName, CancellationToken cancellationToken = default);
}
