namespace OAI.Web.Services.Uploads;

public sealed record MobileUploadApiResponse(
    Guid UploadBatchId,
    string BatchCode,
    int TotalFiles,
    string Status,
    string? BackgroundJobId,
    IReadOnlyList<MobileUploadApiFileResponse> Files);

public sealed record MobileUploadApiFileResponse(
    Guid UploadBatchFileId,
    string OriginalFileName,
    string StoredFilePath,
    string ContentType,
    long FileSizeBytes,
    string Status);
