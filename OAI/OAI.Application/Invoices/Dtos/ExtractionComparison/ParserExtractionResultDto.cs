namespace OAI.Application.Invoices.Dtos.ExtractionComparison;

public sealed record ParserExtractionResultDto
{
    public bool IsAvailable { get; init; }

    public string EngineName { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public string VendorName { get; init; } = string.Empty;

    public string InvoiceNumber { get; init; } = string.Empty;

    public DateOnly? IssueDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal? DeclaredSubtotal { get; init; }

    public decimal? DeclaredTaxAmount { get; init; }

    public decimal? DeclaredTotalAmount { get; init; }

    public int LineItemCount { get; init; }
}