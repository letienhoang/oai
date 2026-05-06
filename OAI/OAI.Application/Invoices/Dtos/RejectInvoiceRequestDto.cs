namespace OAI.Application.Invoices.Dtos;

public sealed record RejectInvoiceRequestDto
{
    public Guid InvoiceId { get; init; }
}