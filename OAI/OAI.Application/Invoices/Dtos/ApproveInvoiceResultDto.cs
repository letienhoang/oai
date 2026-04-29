namespace OAI.Application.Invoices.Dtos;

public sealed record ApproveInvoiceResultDto
{
    public Guid InvoiceId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}