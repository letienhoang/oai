namespace OAI.Api.Contracts.Uploads;

public sealed record UploadUnsupportedFileResponse(
    string FileName,
    string FileType,
    string Status,
    string Message);