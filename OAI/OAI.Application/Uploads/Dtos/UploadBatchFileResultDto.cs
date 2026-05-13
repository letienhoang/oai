using OAI.Domain.Enums;

namespace OAI.Application.Uploads.Dtos;

public sealed record UploadBatchFileResultDto(
    Guid UploadBatchFileId,
    string OriginalFileName,
    string StoredFilePath,
    string ContentType,
    long FileSizeBytes,
    UploadBatchFileStatus Status);