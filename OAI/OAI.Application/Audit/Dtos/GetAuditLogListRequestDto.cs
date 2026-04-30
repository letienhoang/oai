namespace OAI.Application.Audit.Dtos;

public sealed record GetAuditLogListRequestDto
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Keyword { get; init; }

    public string? EntityName { get; init; }

    public string? ActionType { get; init; }
}