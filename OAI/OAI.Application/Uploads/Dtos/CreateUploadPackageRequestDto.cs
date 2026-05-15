namespace OAI.Application.Uploads.Dtos;

public sealed record CreateUploadPackageRequestDto(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content,
    Guid? UploadedByUserId = null,
    string? UploadedByUserName = null);