namespace OAI.Application.Files;

public sealed record DownloadableFileResult(
    bool Succeeded,
    string? PhysicalPath,
    string? FileName,
    string? ContentType,
    long? FileSizeBytes,
    string? ErrorMessage,
    DownloadableFileErrorCode? ErrorCode);

public enum DownloadableFileErrorCode
{
    NotFound = 1,
    PhysicalFileMissing = 2,
    UnsafePath = 3,
    Unsupported = 4,
    UnsupportedContentType = 5
}
