namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceLineItemRequestDto
{
    public int LineNo { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
}