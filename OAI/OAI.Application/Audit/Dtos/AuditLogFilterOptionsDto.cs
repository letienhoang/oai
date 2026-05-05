namespace OAI.Application.Audit.Dtos;

public sealed record AuditLogFilterOptionsDto
{
    public IReadOnlyList<string> EntityNames { get; init; } = [];

    public IReadOnlyList<string> ActionTypes { get; init; } = [];

    public IReadOnlyList<string> Sources { get; init; } = [];
}
