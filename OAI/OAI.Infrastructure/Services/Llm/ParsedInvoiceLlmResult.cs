namespace OAI.Infrastructure.Services.Llm;

public sealed record ParsedInvoiceLlmResult
{
    public string VendorName { get; init; } = string.Empty;
    public string? VendorTaxNumber { get; init; }
    public string? VendorAddress { get; init; }
    public string? VendorEmail { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;
    public string IssueDate { get; init; } = string.Empty;
    public string? DueDate { get; init; }
    public string Currency { get; init; } = "VND";

    public decimal DeclaredSubtotal { get; init; }
    public decimal DeclaredTaxAmount { get; init; }
    public decimal DeclaredTotalAmount { get; init; }

    public List<ParsedInvoiceLineItemLlmResult> LineItems { get; init; } = new();
}

public sealed record ParsedInvoiceLineItemLlmResult
{
    public int LineNo { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
}