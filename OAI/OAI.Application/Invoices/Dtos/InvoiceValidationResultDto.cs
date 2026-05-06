namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceValidationResultDto
{
    public Guid InvoiceId { get; init; }
    public bool IsValid { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public List<ValidationIssueDto> Issues { get; init; } = new();
}