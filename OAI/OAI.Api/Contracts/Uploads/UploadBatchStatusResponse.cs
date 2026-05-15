namespace OAI.Api.Contracts.Uploads;

public sealed record UploadBatchStatusResponse(
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
    IReadOnlyList<UploadBatchFileStatusResponse> Files);