namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceExtractionResultDto
{
    public Guid ExtractionResultId { get; init; }
    public string EngineName { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
    public DateTimeOffset ExtractedAt { get; init; }
    public bool IsSuccessful { get; init; }
    public int AttemptNo { get; init; }
}