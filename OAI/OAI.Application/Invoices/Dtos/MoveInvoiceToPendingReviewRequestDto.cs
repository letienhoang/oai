namespace OAI.Application.Invoices.Dtos;

public sealed record MoveInvoiceToPendingReviewRequestDto
{
    public Guid InvoiceId { get; init; }
}