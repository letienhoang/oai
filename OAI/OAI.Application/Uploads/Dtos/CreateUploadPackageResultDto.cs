using OAI.Domain.Enums;

namespace OAI.Application.Uploads.Dtos;

public sealed record CreateUploadPackageResultDto(
    Guid UploadBatchId,
    string BatchCode,
    int TotalFiles,
    UploadBatchStatus Status,
    IReadOnlyList<UploadBatchFileResultDto> Files);