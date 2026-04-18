namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceCreateRequestDto
{
    public Guid VendorId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateOnly IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public string Currency { get; init; } = "VND";

    public decimal DeclaredSubtotal { get; init; }
    public decimal DeclaredTaxAmount { get; init; }
    public decimal DeclaredTotalAmount { get; init; }

    public string? SourceFileName { get; init; }

    public List<InvoiceLineItemDto> LineItems { get; init; } = new();
}