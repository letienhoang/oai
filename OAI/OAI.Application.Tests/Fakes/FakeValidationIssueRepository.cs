using OAI.Application.Abstractions.Persistence;
using OAI.Application.Dashboard.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Enums;

namespace OAI.Application.Tests.Fakes;

public sealed class FakeValidationIssueRepository : IValidationIssueRepository
{
    private readonly List<ValidationIssue> _issues = new();

    public IReadOnlyList<ValidationIssue> Issues => _issues.AsReadOnly();

    public Task<IReadOnlyList<ValidationIssue>> GetByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var result = _issues
            .Where(x => x.InvoiceId == invoiceId)
            .OrderByDescending(x => x.DetectedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<ValidationIssue>>(result);
    }

    public Task<IReadOnlyList<ValidationIssue>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        string? severity = null,
        bool? isResolved = null,
        CancellationToken cancellationToken = default)
    {
        var result = ApplyFilters(_issues, keyword, severity, isResolved)
            .OrderByDescending(x => x.DetectedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<ValidationIssue>>(result);
    }

    public Task<int> CountAsync(
        string? keyword = null,
        string? severity = null,
        bool? isResolved = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplyFilters(_issues, keyword, severity, isResolved).Count());
    }

    public Task<int> CountOpenAsync(CancellationToken cancellationToken = default)
    {
        return CountOpenAsync(new DashboardFilterDto(), cancellationToken);
    }

    public Task<int> CountOpenAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplyDashboardFilter(_issues, filter).Count(x => !x.IsResolved));
    }

    public Task<int> CountResolvedAsync(CancellationToken cancellationToken = default)
    {
        return CountResolvedAsync(new DashboardFilterDto(), cancellationToken);
    }

    public Task<int> CountResolvedAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplyDashboardFilter(_issues, filter).Count(x => x.IsResolved));
    }

    public Task<IReadOnlyList<ValidationIssue>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetRecentAsync(take, new DashboardFilterDto(), cancellationToken);
    }

    public Task<IReadOnlyList<ValidationIssue>> GetRecentAsync(
        int take,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = ApplyDashboardFilter(_issues, filter)
            .OrderByDescending(x => x.DetectedAt)
            .Take(take)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<ValidationIssue>>(result);
    }

    public Task AddAsync(
        ValidationIssue issue,
        CancellationToken cancellationToken = default)
    {
        _issues.Add(issue);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(
        IEnumerable<ValidationIssue> issues,
        CancellationToken cancellationToken = default)
    {
        _issues.AddRange(issues);
        return Task.CompletedTask;
    }

    public Task MarkResolvedAsync(
        Guid validationIssueId,
        CancellationToken cancellationToken = default)
    {
        _issues.FirstOrDefault(x => x.Id == validationIssueId)?.Resolve();
        return Task.CompletedTask;
    }

    public void Seed(ValidationIssue issue, Invoice? invoice = null)
    {
        if (invoice is not null)
            SetInvoice(issue, invoice);

        _issues.Add(issue);
    }

    private static IEnumerable<ValidationIssue> ApplyFilters(
        IEnumerable<ValidationIssue> issues,
        string? keyword,
        string? severity,
        bool? isResolved)
    {
        var query = issues;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim();

            query = query.Where(x =>
                x.FieldName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                x.RuleCode.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                x.Message.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (x.Invoice?.InvoiceNumber.Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Invoice?.Vendor?.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(severity) &&
            Enum.TryParse<ValidationSeverity>(severity, ignoreCase: true, out var severityValue))
        {
            query = query.Where(x => x.Severity == severityValue);
        }

        if (isResolved.HasValue)
            query = query.Where(x => x.IsResolved == isResolved.Value);

        return query;
    }

    private static IEnumerable<ValidationIssue> ApplyDashboardFilter(
        IEnumerable<ValidationIssue> issues,
        DashboardFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = issues;

        if (filter.VendorId.HasValue)
        {
            query = query.Where(x =>
                x.Invoice is not null &&
                x.Invoice.VendorId == filter.VendorId.Value);
        }

        if (filter.IssueDateFrom.HasValue)
        {
            query = query.Where(x =>
                x.Invoice is not null &&
                x.Invoice.IssueDate >= filter.IssueDateFrom.Value);
        }

        if (filter.IssueDateTo.HasValue)
        {
            query = query.Where(x =>
                x.Invoice is not null &&
                x.Invoice.IssueDate <= filter.IssueDateTo.Value);
        }

        return query;
    }

    private static void SetInvoice(ValidationIssue issue, Invoice invoice)
    {
        typeof(ValidationIssue)
            .GetProperty(nameof(ValidationIssue.Invoice))!
            .SetValue(issue, invoice);
    }
}
