namespace OAI.Api.Contracts.Uploads;

public sealed record UploadBatchFileStatusResponse(
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