namespace OAI.Application.Files;

public interface IFileDownloadService
{
    Task<DownloadableFileResult> GetDownloadableFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);

    Task<DownloadableFileResult> GetPreviewableFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);
}
