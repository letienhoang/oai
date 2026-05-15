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
    public string? SourceFilePath { get; init; }
    
    public Guid? UploadBatchFileId { get; init; }
    public string? SourceFileContentType { get; init; }
    public long? SourceFileSizeBytes { get; init; }
    
    public decimal? ExtractionConfidenceScore { get; init; }
    public string? ExtractionEngineName { get; init; }
    public string? ExtractionRawText { get; init; }
    public string? ExtractionStructuredJson { get; init; }

    public List<InvoiceLineItemRequestDto> LineItems { get; init; } = new();
}