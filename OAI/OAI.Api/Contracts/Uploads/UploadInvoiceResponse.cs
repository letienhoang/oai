namespace OAI.Api.Contracts.Uploads;

public sealed record UploadInvoiceResponse(
    Guid InvoiceId,
    string FileName,
    string Status,
    string? Message,
    string? MessageCode);