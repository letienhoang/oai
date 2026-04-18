namespace OAI.Application.Invoices.Dtos;

public sealed record ValidateInvoiceRequestDto
{
    public Guid InvoiceId { get; init; }
    public decimal Tolerance { get; init; } = 0.01m;
}