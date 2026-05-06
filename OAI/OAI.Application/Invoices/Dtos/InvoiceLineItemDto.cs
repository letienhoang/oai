namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceLineItemDto
{
    public Guid InvoiceLineItemId { get; init; }
    
    public int LineNo { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
    public decimal NetAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrossAmount { get; init; }
}