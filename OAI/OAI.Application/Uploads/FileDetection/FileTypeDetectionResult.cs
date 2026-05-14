namespace OAI.Application.Uploads.FileDetection;

public sealed record FileTypeDetectionResult(
    DetectedUploadFileType FileType,
    string? NormalizedExtension,
    string? NormalizedContentType,
    string Reason)
{
    public bool IsSupported => FileType != DetectedUploadFileType.Unsupported;
}
