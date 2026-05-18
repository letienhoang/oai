namespace OAI.Web.Services.Uploads;

public sealed record MobileUploadBatchStatusResponse(
    Guid UploadBatchId,
    string BatchCode,
    string Status,
    int TotalFiles,
    int ProcessedFiles,
    int FailedFiles,
    int PendingFiles,
    int ProcessingFiles,
    int RetryPendingFiles,
    int UnsupportedFiles,
    string? UploadedByUserName,
    string? OriginalZipFilePath,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<MobileUploadBatchFileStatusResponse> Files);

public sealed record MobileUploadBatchFileStatusResponse(
    Guid UploadBatchFileId,
    Guid UploadBatchId,
    string OriginalFileName,
    string StoredFilePath,
    string ContentType,
    long FileSizeBytes,
    string Status,
    Guid? InvoiceId,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? ProcessingCompletedAt);
