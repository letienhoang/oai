namespace OAI.Application.Invoices.Dtos.ExtractionComparison;

public sealed record FieldComparisonDto
{
    public string FieldName { get; init; } = string.Empty;

    public string RuleBasedValue { get; init; } = string.Empty;

    public string AiValue { get; init; } = string.Empty;

    public bool IsSame { get; init; }
}