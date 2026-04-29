namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceLineItemRequestDto
{
    public Guid? InvoiceLineItemId { get; init; }
    
    public int LineNo { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
}