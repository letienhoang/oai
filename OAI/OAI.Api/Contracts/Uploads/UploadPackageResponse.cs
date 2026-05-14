namespace OAI.Api.Contracts.Uploads;

public sealed record UploadPackageResponse(
    Guid UploadBatchId,
    string BatchCode,
    int TotalFiles,
    string Status,
    IReadOnlyList<UploadPackageFileResponse> Files);