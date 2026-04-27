namespace OAI.Application.Invoices.Dtos;

public sealed record ExtractedInvoiceDto
{
    public string VendorName { get; init; } = string.Empty;
    public string? VendorTaxNumber { get; init; }
    public string? VendorAddress { get; init; }
    public string? VendorEmail { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;
    public DateOnly IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public string Currency { get; init; } = "VND";

    public decimal DeclaredSubtotal { get; init; }
    public decimal DeclaredTaxAmount { get; init; }
    public decimal DeclaredTotalAmount { get; init; }

    public decimal ConfidenceScore { get; init; }
    public string EngineName { get; init; } = "Tesseract";
    public string? RawText { get; init; }

    public List<InvoiceLineItemRequestDto> LineItems { get; init; } = new();
}