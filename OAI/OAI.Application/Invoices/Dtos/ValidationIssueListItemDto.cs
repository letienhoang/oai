namespace OAI.Application.Invoices.Dtos;

public sealed record ValidationIssueListItemDto
{
    public Guid ValidationIssueId { get; init; }
    public Guid InvoiceId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;
    public string VendorName { get; init; } = string.Empty;

    public string FieldName { get; init; } = string.Empty;
    public string RuleCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? MessageCode { get; init; }
    public IReadOnlyDictionary<string, string>? MessageParameters { get; init; }
    public string Severity { get; init; } = string.Empty;

    public bool IsResolved { get; init; }
    public DateTimeOffset DetectedAt { get; init; }
}
