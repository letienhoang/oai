using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
using OAI.Domain.Audit;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly OaiDbContext _context;

    public AuditLogRepository(OaiDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        string? entityName = null,
        string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        query = ApplyFilters(query, keyword, entityName, actionType);

        return await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        string? keyword = null,
        string? entityName = null,
        string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        query = ApplyFilters(query, keyword, entityName, actionType);

        return await query.CountAsync(cancellationToken);
    }

    private static IQueryable<AuditLogEntry> ApplyFilters(
        IQueryable<AuditLogEntry> query,
        string? keyword,
        string? entityName,
        string? actionType)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim();

            query = query.Where(x =>
                x.EntityName.Contains(normalized) ||
                (x.EntityId != null && x.EntityId.Contains(normalized)) ||
                (x.UserName != null && x.UserName.Contains(normalized)) ||
                (x.UserId != null && x.UserId.Contains(normalized)) ||
                (x.Source != null && x.Source.Contains(normalized)) ||
                (x.OldValuesJson != null && x.OldValuesJson.Contains(normalized)) ||
                (x.NewValuesJson != null && x.NewValuesJson.Contains(normalized)));
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var normalizedEntity = entityName.Trim();

            query = query.Where(x => x.EntityName == normalizedEntity);
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            var normalizedAction = actionType.Trim();

            query = query.Where(x => x.ActionType.ToString() == normalizedAction);
        }

        return query;
    }
}