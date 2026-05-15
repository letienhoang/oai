namespace OAI.Application.Uploads.FileDetection;

public interface IFileTypeDetectionService
{
    Task<FileTypeDetectionResult> DetectAsync(
        Stream fileStream,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}
