namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceListItemDto
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string VendorName { get; init; } = string.Empty;
    public DateOnly IssueDate { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public int LineItemCount { get; init; }
}