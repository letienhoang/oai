namespace OAI.Application.Audit.Dtos;

public sealed record AuditLogFilterDto
{
    public string? Keyword { get; init; }

    public string? EntityName { get; init; }

    public string? ActionType { get; init; }

    public string? UserName { get; init; }

    public string? Source { get; init; }

    public DateOnly? OccurredAtFrom { get; init; }

    public DateOnly? OccurredAtTo { get; init; }

    public string? SortBy { get; init; } = AuditLogSortFields.OccurredAt;

    public bool SortDescending { get; init; } = true;
}
