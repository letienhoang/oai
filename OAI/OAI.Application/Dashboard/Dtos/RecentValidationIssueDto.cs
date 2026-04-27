namespace OAI.Application.Dashboard.Dtos;

public sealed record RecentValidationIssueDto
{
    public Guid ValidationIssueId { get; init; }

    public Guid InvoiceId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public string RuleCode { get; init; } = string.Empty;

    public string FieldName { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public bool IsResolved { get; init; }

    public DateTimeOffset DetectedAt { get; init; }
}