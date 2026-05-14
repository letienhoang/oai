using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Audit.Dtos;
using OAI.Domain.Audit;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly OaiDbContext _context;
    private readonly IDbContextFactory<OaiDbContext> _dbContextFactory;

    public AuditLogRepository(
        OaiDbContext context,
        IDbContextFactory<OaiDbContext> dbContextFactory)
    {
        _context = context;
        _dbContextFactory = dbContextFactory;
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        string? entityName = null,
        string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        return GetPagedAsync(
            pageNumber,
            pageSize,
            new AuditLogFilterDto
            {
                Keyword = keyword,
                EntityName = entityName,
                ActionType = actionType
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.AuditLogs.AsNoTracking();

        query = ApplyFilter(query, filter);
        query = ApplySorting(query, filter);

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        string? keyword = null,
        string? entityName = null,
        string? actionType = null,
        CancellationToken cancellationToken = default)
    {
        return CountAsync(
            new AuditLogFilterDto
            {
                Keyword = keyword,
                EntityName = entityName,
                ActionType = actionType
            },
            cancellationToken);
    }

    public async Task<int> CountAsync(
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.AuditLogs.AsNoTracking();

        query = ApplyFilter(query, filter);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetEntityNameOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.AuditLogs
            .AsNoTracking()
            .Select(x => x.EntityName.Trim())
            .Where(x => x != string.Empty)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetActionTypeOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var actionTypes = await context.AuditLogs
            .AsNoTracking()
            .Select(x => x.ActionType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return actionTypes
            .Select(x => x.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OrderBy(x => x)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetSourceOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.AuditLogs
            .AsNoTracking()
            .Where(x => x.Source != null)
            .Select(x => x.Source!.Trim())
            .Where(x => x != string.Empty)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<AuditLogEntry> ApplyFilter(
        IQueryable<AuditLogEntry> query,
        AuditLogFilterDto filter)
    {
        filter ??= new AuditLogFilterDto();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var normalized = filter.Keyword.Trim();
            var matchingActions = Enum.GetValues<AuditActionType>()
                .Where(x => x.ToString().Contains(normalized, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            query = query.Where(x =>
                x.EntityName.Contains(normalized) ||
                (x.EntityId != null && x.EntityId.Contains(normalized)) ||
                matchingActions.Contains(x.ActionType) ||
                (x.UserName != null && x.UserName.Contains(normalized)) ||
                (x.Source != null && x.Source.Contains(normalized)));
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            var normalizedEntity = filter.EntityName.Trim();

            query = query.Where(x => x.EntityName == normalizedEntity);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActionType)
            && Enum.TryParse<AuditActionType>(filter.ActionType.Trim(), ignoreCase: true, out var actionType))
        {
            query = query.Where(x => x.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(filter.UserName))
        {
            var normalizedUserName = filter.UserName.Trim();

            query = query.Where(x => x.UserName != null && x.UserName.Contains(normalizedUserName));
        }

        if (!string.IsNullOrWhiteSpace(filter.Source))
        {
            var normalizedSource = filter.Source.Trim();

            query = query.Where(x => x.Source == normalizedSource);
        }

        if (filter.OccurredAtFrom.HasValue)
        {
            var occurredAtFrom = new DateTimeOffset(
                filter.OccurredAtFrom.Value.ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);

            query = query.Where(x => x.OccurredAt >= occurredAtFrom);
        }

        if (filter.OccurredAtTo.HasValue)
        {
            var occurredAtTo = new DateTimeOffset(
                filter.OccurredAtTo.Value.ToDateTime(TimeOnly.MaxValue),
                TimeSpan.Zero);

            query = query.Where(x => x.OccurredAt <= occurredAtTo);
        }

        return query;
    }

    private static IQueryable<AuditLogEntry> ApplySorting(
        IQueryable<AuditLogEntry> query,
        AuditLogFilterDto filter)
    {
        filter ??= new AuditLogFilterDto();

        var sortBy = string.IsNullOrWhiteSpace(filter.SortBy)
            ? AuditLogSortFields.OccurredAt
            : filter.SortBy.Trim();

        return sortBy switch
        {
            AuditLogSortFields.EntityName => filter.SortDescending
                ? query.OrderByDescending(x => x.EntityName).ThenByDescending(x => x.OccurredAt)
                : query.OrderBy(x => x.EntityName).ThenByDescending(x => x.OccurredAt),
            AuditLogSortFields.ActionType => filter.SortDescending
                ? query.OrderByDescending(x => x.ActionType).ThenByDescending(x => x.OccurredAt)
                : query.OrderBy(x => x.ActionType).ThenByDescending(x => x.OccurredAt),
            AuditLogSortFields.UserName => filter.SortDescending
                ? query.OrderByDescending(x => x.UserName).ThenByDescending(x => x.OccurredAt)
                : query.OrderBy(x => x.UserName).ThenByDescending(x => x.OccurredAt),
            AuditLogSortFields.Source => filter.SortDescending
                ? query.OrderByDescending(x => x.Source).ThenByDescending(x => x.OccurredAt)
                : query.OrderBy(x => x.Source).ThenByDescending(x => x.OccurredAt),
            _ => filter.SortDescending
                ? query.OrderByDescending(x => x.OccurredAt)
                : query.OrderBy(x => x.OccurredAt)
        };
    }
}
