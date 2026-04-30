namespace OAI.Domain.Audit;

public sealed class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    public AuditActionType ActionType { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }

    public string? UserId { get; set; }
    public string? UserName { get; set; }

    public string? CorrelationId { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Source { get; set; }
}