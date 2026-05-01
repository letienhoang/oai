namespace OAI.Application.Invoices.Dtos;

public sealed record ValidationIssueDto
{
    public Guid ValidationIssueId { get; init; }
    public string FieldName { get; init; } = string.Empty;
    public string RuleCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? MessageCode { get; init; }
    public IReadOnlyDictionary<string, string>? MessageParameters { get; init; }
    public string Severity { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
    public DateTimeOffset DetectedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
}
