namespace OAI.Application.Invoices.Dtos;

public sealed record GetValidationIssueListRequestDto
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    public string? Keyword { get; init; }
    public string? Severity { get; init; }
    public bool? IsResolved { get; init; }
}