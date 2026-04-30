using OAI.Domain.Audit;

namespace OAI.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLogEntry>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        string? entityName = null,
        string? actionType = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        string? keyword = null,
        string? entityName = null,
        string? actionType = null,
        CancellationToken cancellationToken = default);
}