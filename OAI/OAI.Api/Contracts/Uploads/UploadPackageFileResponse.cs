namespace OAI.Api.Contracts.Uploads;

public sealed record UploadPackageFileResponse(
    Guid UploadBatchFileId,
    string OriginalFileName,
    string StoredFilePath,
    string ContentType,
    long FileSizeBytes,
    string Status);