namespace OAI.Api.Contracts.Uploads;

public sealed class UploadInvoiceRequest
{
    public IFormFile? File { get; set; }

    public string? UploadedByUserId { get; set; }

    public string? UploadedByUserName { get; set; }
}
