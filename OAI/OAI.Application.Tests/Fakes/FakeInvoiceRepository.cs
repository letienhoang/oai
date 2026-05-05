using OAI.Application.Abstractions.Persistence;
using OAI.Application.Dashboard.Dtos;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Enums;

namespace OAI.Application.Tests.Fakes;

public sealed class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly List<Invoice> _invoices = new();

    public IReadOnlyList<Invoice> Invoices => _invoices.AsReadOnly();

    public Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_invoices.FirstOrDefault(x => x.Id == id));
    }

    public Task<Invoice?> GetByInvoiceNumberAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_invoices.FirstOrDefault(x =>
            string.Equals(x.InvoiceNumber, invoiceNumber, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IReadOnlyList<Invoice>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        InvoiceListFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = ApplyListFilter(_invoices, filter)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Invoice>>(result);
    }

    public Task<IReadOnlyList<Invoice>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        return GetPagedAsync(
            pageNumber,
            pageSize,
            new InvoiceListFilterDto { Keyword = keyword },
            cancellationToken);
    }

    public Task<int> CountAsync(
        InvoiceListFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplyListFilter(_invoices, filter).Count());
    }

    public Task<int> CountAsync(
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        return CountAsync(
            new InvoiceListFilterDto { Keyword = keyword },
            cancellationToken);
    }

    public Task<int> CountByStatusAsync(
        InvoiceStatus status,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplyDashboardFilter(_invoices, filter).Count(x => x.Status == status));
    }

    public Task<int> CountByStatusAsync(
        InvoiceStatus status,
        CancellationToken cancellationToken = default)
    {
        return CountByStatusAsync(status, new DashboardFilterDto(), cancellationToken);
    }

    public Task<int> CountWithValidationIssuesAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplyDashboardFilter(_invoices, filter).Count(x =>
            x.ValidationIssues.Any(v => !v.IsResolved)));
    }

    public Task<int> CountWithValidationIssuesAsync(
        CancellationToken cancellationToken = default)
    {
        return CountWithValidationIssuesAsync(new DashboardFilterDto(), cancellationToken);
    }

    public Task<IReadOnlyList<Invoice>> GetRecentAsync(
        int take,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = ApplyDashboardFilter(_invoices, filter)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Invoice>>(result);
    }

    public Task<IReadOnlyList<Invoice>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetRecentAsync(take, new DashboardFilterDto(), cancellationToken);
    }

    public Task AddAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        _invoices.Add(invoice);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        _invoices.Remove(invoice);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByInvoiceNumberAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        var exists = _invoices.Any(x =>
            string.Equals(x.InvoiceNumber, invoiceNumber, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public void Seed(Invoice invoice)
    {
        _invoices.Add(invoice);
    }

    private static IEnumerable<Invoice> ApplyListFilter(
        IEnumerable<Invoice> invoices,
        InvoiceListFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = invoices;

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var normalized = filter.Keyword.Trim();

            query = query.Where(x =>
                x.InvoiceNumber.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (x.Vendor?.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.VendorId.HasValue)
            query = query.Where(x => x.VendorId == filter.VendorId.Value);

        if (filter.IssueDateFrom.HasValue)
            query = query.Where(x => x.IssueDate >= filter.IssueDateFrom.Value);

        if (filter.IssueDateTo.HasValue)
            query = query.Where(x => x.IssueDate <= filter.IssueDateTo.Value);

        if (filter.TotalAmountFrom.HasValue)
            query = query.Where(x => x.DeclaredTotalAmount.Amount >= filter.TotalAmountFrom.Value);

        if (filter.TotalAmountTo.HasValue)
            query = query.Where(x => x.DeclaredTotalAmount.Amount <= filter.TotalAmountTo.Value);

        if (filter.HasOpenValidationIssues.HasValue)
        {
            query = filter.HasOpenValidationIssues.Value
                ? query.Where(x => x.ValidationIssues.Any(v => !v.IsResolved))
                : query.Where(x => !x.ValidationIssues.Any(v => !v.IsResolved));
        }

        return ApplyListSorting(query, filter);
    }

    private static IEnumerable<Invoice> ApplyDashboardFilter(
        IEnumerable<Invoice> invoices,
        DashboardFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = invoices;

        if (filter.VendorId.HasValue)
            query = query.Where(x => x.VendorId == filter.VendorId.Value);

        if (filter.IssueDateFrom.HasValue)
            query = query.Where(x => x.IssueDate >= filter.IssueDateFrom.Value);

        if (filter.IssueDateTo.HasValue)
            query = query.Where(x => x.IssueDate <= filter.IssueDateTo.Value);

        return query;
    }

    private static IOrderedEnumerable<Invoice> ApplyListSorting(
        IEnumerable<Invoice> invoices,
        InvoiceListFilterDto filter)
    {
        var sortBy = filter.SortBy?.Trim();

        return sortBy switch
        {
            "CreatedAt" => filter.SortDescending
                ? invoices.OrderByDescending(x => x.CreatedAt)
                : invoices.OrderBy(x => x.CreatedAt),
            "InvoiceNumber" => filter.SortDescending
                ? invoices.OrderByDescending(x => x.InvoiceNumber)
                : invoices.OrderBy(x => x.InvoiceNumber),
            "TotalAmount" => filter.SortDescending
                ? invoices.OrderByDescending(x => x.DeclaredTotalAmount.Amount)
                : invoices.OrderBy(x => x.DeclaredTotalAmount.Amount),
            _ => filter.SortDescending
                ? invoices.OrderByDescending(x => x.IssueDate).ThenByDescending(x => x.CreatedAt)
                : invoices.OrderBy(x => x.IssueDate).ThenByDescending(x => x.CreatedAt)
        };
    }
}
