namespace OAI.Application.Abstractions.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default);
}