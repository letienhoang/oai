namespace OAI.Application.Audit.Dtos;

public sealed record AuditLogListItemDto
{
    public Guid Id { get; init; }

    public string EntityName { get; init; } = string.Empty;

    public string? EntityId { get; init; }

    public string ActionType { get; init; } = string.Empty;

    public string? UserId { get; init; }

    public string? UserName { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public string? Source { get; init; }

    public string? OldValuesJson { get; init; }

    public string? NewValuesJson { get; init; }
}