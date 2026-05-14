namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceDetailDto
{
    public Guid InvoiceId { get; init; }
    public Guid VendorId { get; init; }
    public string VendorName { get; init; } = string.Empty;

    public string InvoiceNumber { get; init; } = string.Empty;
    public DateOnly IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public string Currency { get; init; } = string.Empty;

    public decimal DeclaredSubtotal { get; init; }
    public decimal DeclaredTaxAmount { get; init; }
    public decimal DeclaredTotalAmount { get; init; }

    public string Status { get; init; } = string.Empty;
    public string? SourceFileName { get; init; }

    public List<InvoiceLineItemDto> LineItems { get; init; } = new();
    public List<ValidationIssueDto> ValidationIssues { get; init; } = new();
    public List<InvoiceExtractionResultDto> ExtractionResults { get; init; } = new();
    public List<InvoiceSourceFileDto> SourceFiles { get; init; } = new();
}

public sealed record InvoiceSourceFileDto(
    Guid Id,
    string? OriginalFileName,
    string? ContentType,
    long FileSizeBytes,
    int? PageNumber,
    DateTimeOffset CreatedAt);
