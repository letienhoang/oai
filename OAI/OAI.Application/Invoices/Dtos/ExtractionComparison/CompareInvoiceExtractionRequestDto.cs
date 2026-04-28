namespace OAI.Application.Invoices.Dtos.ExtractionComparison;

public sealed record CompareInvoiceExtractionRequestDto
{
    public Guid InvoiceId { get; init; }
}