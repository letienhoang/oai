namespace OAI.Application.Invoices.Dtos;

public sealed record ApproveInvoiceRequestDto
{
    public Guid InvoiceId { get; init; }
}