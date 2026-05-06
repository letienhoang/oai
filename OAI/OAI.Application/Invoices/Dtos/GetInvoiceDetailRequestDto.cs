namespace OAI.Application.Invoices.Dtos;

public sealed record GetInvoiceDetailRequestDto
{
    public Guid InvoiceId { get; init; }
}