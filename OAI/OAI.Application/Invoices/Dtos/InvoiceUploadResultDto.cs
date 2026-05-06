namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceUploadResultDto
{
    public Guid InvoiceId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? MessageCode { get; init; }
    public IReadOnlyDictionary<string, string>? MessageParameters { get; init; }
}
