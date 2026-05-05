namespace OAI.Application.Audit.Dtos;

public sealed record GetAuditLogListRequestDto
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public AuditLogFilterDto Filter { get; init; } = new();

    public string? Keyword
    {
        get => Filter.Keyword;
        init => Filter = Filter with { Keyword = value };
    }

    public string? EntityName
    {
        get => Filter.EntityName;
        init => Filter = Filter with { EntityName = value };
    }

    public string? ActionType
    {
        get => Filter.ActionType;
        init => Filter = Filter with { ActionType = value };
    }
}
