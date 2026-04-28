namespace OAI.Application.Invoices.Dtos.ExtractionComparison;

public sealed record InvoiceExtractionComparisonDto
{
    public Guid InvoiceId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public string RawText { get; init; } = string.Empty;

    public ParserExtractionResultDto RuleBasedResult { get; init; } = new();

    public ParserExtractionResultDto AiResult { get; init; } = new();

    public List<FieldComparisonDto> FieldComparisons { get; init; } = new();
}