using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Dashboard.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Repositories;

public sealed class ValidationIssueRepository : IValidationIssueRepository
{
    private readonly OaiDbContext _context;

    public ValidationIssueRepository(OaiDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ValidationIssue>> GetByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ValidationIssues
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .OrderByDescending(x => x.DetectedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ValidationIssue>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        string? severity = null,
        bool? isResolved = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ValidationIssue> query = _context.ValidationIssues
            .AsNoTracking()
            .Include(x => x.Invoice)
                .ThenInclude(x => x!.Vendor);

        query = ApplyFilters(query, keyword, severity, isResolved);

        return await query
            .OrderByDescending(x => x.DetectedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        string? keyword = null,
        string? severity = null,
        bool? isResolved = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ValidationIssue> query = _context.ValidationIssues
            .AsNoTracking()
            .Include(x => x.Invoice)
                .ThenInclude(x => x!.Vendor);

        query = ApplyFilters(query, keyword, severity, isResolved);

        return await query.CountAsync(cancellationToken);
    }

    public Task<int> CountOpenAsync(CancellationToken cancellationToken = default)
    {
        return CountOpenAsync(new DashboardFilterDto(), cancellationToken);
    }

    public async Task<int> CountOpenAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDashboardFilter(_context.ValidationIssues.AsNoTracking(), filter);

        return await query.CountAsync(x => !x.IsResolved, cancellationToken);
    }

    public Task<int> CountResolvedAsync(CancellationToken cancellationToken = default)
    {
        return CountResolvedAsync(new DashboardFilterDto(), cancellationToken);
    }

    public async Task<int> CountResolvedAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDashboardFilter(_context.ValidationIssues.AsNoTracking(), filter);

        return await query.CountAsync(x => x.IsResolved, cancellationToken);
    }

    public Task<IReadOnlyList<ValidationIssue>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetRecentAsync(take, new DashboardFilterDto(), cancellationToken);
    }

    public async Task<IReadOnlyList<ValidationIssue>> GetRecentAsync(
        int take,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDashboardFilter(_context.ValidationIssues, filter)
            .AsNoTracking()
            .Include(x => x.Invoice)
                .ThenInclude(x => x!.Vendor);

        return await query
            .OrderByDescending(x => x.DetectedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        ValidationIssue issue,
        CancellationToken cancellationToken = default)
    {
        await _context.ValidationIssues.AddAsync(issue, cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<ValidationIssue> issues,
        CancellationToken cancellationToken = default)
    {
        await _context.ValidationIssues.AddRangeAsync(issues, cancellationToken);
    }

    public async Task MarkResolvedAsync(
        Guid validationIssueId,
        CancellationToken cancellationToken = default)
    {
        var issue = await _context.ValidationIssues
            .FirstOrDefaultAsync(x => x.Id == validationIssueId, cancellationToken);

        if (issue is null)
            return;

        issue.Resolve();
    }

    private static IQueryable<ValidationIssue> ApplyFilters(
        IQueryable<ValidationIssue> query,
        string? keyword,
        string? severity,
        bool? isResolved)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim();

            query = query.Where(x =>
                x.FieldName.Contains(normalized) ||
                x.RuleCode.Contains(normalized) ||
                x.Message.Contains(normalized) ||
                (x.Invoice != null && x.Invoice.InvoiceNumber.Contains(normalized)) ||
                (x.Invoice != null && x.Invoice.Vendor != null && x.Invoice.Vendor.Name.Contains(normalized)));
        }

        if (!string.IsNullOrWhiteSpace(severity) &&
            Enum.TryParse<ValidationSeverity>(severity, ignoreCase: true, out var severityValue))
        {
            query = query.Where(x => x.Severity == severityValue);
        }

        if (isResolved.HasValue)
        {
            query = query.Where(x => x.IsResolved == isResolved.Value);
        }

        return query;
    }

    private static IQueryable<ValidationIssue> ApplyDashboardFilter(
        IQueryable<ValidationIssue> query,
        DashboardFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.VendorId.HasValue)
        {
            query = query.Where(x =>
                x.Invoice != null &&
                x.Invoice.VendorId == filter.VendorId.Value);
        }

        if (filter.IssueDateFrom.HasValue)
        {
            query = query.Where(x =>
                x.Invoice != null &&
                x.Invoice.IssueDate >= filter.IssueDateFrom.Value);
        }

        if (filter.IssueDateTo.HasValue)
        {
            query = query.Where(x =>
                x.Invoice != null &&
                x.Invoice.IssueDate <= filter.IssueDateTo.Value);
        }

        return query;
    }
}
