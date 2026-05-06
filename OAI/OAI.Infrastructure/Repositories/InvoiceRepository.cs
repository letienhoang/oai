using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Dashboard.Dtos;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly OaiDbContext _context;

    public InvoiceRepository(OaiDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(x => x.Vendor)
            .Include(x => x.LineItems)
            .Include(x => x.ValidationIssues)
            .Include(x => x.ExtractionResults)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return null;

        var normalized = invoiceNumber.Trim();

        return await _context.Invoices
            .Include(x => x.Vendor)
            .Include(x => x.LineItems)
            .Include(x => x.ValidationIssues)
            .Include(x => x.ExtractionResults)
            .FirstOrDefaultAsync(x => x.InvoiceNumber == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        InvoiceListFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Invoice> query = ApplyListFilter(_context.Invoices, filter)
            .AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.LineItems);

        query = ApplyListSorting(query, filter);

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
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

    public async Task<int> CountAsync(InvoiceListFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = ApplyListFilter(_context.Invoices.AsNoTracking(), filter);

        return await query.CountAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? keyword = null, CancellationToken cancellationToken = default)
    {
        return CountAsync(
            new InvoiceListFilterDto { Keyword = keyword },
            cancellationToken);
    }

    public async Task<int> CountByStatusAsync(
        InvoiceStatus status,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDashboardFilter(_context.Invoices.AsNoTracking(), filter);

        return await query.CountAsync(x => x.Status == status, cancellationToken);
    }

    public Task<int> CountByStatusAsync(
        InvoiceStatus status,
        CancellationToken cancellationToken = default)
    {
        return CountByStatusAsync(status, new DashboardFilterDto(), cancellationToken);
    }

    public async Task<int> CountWithValidationIssuesAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDashboardFilter(_context.Invoices.AsNoTracking(), filter);

        return await query.CountAsync(
            x => x.ValidationIssues.Any(v => !v.IsResolved),
            cancellationToken);
    }

    public Task<int> CountWithValidationIssuesAsync(
        CancellationToken cancellationToken = default)
    {
        return CountWithValidationIssuesAsync(new DashboardFilterDto(), cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetRecentAsync(
        int take,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyDashboardFilter(_context.Invoices, filter)
            .AsNoTracking()
            .Include(x => x.Vendor);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Invoice>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetRecentAsync(take, new DashboardFilterDto(), cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        // Entity loaded from the current DbContext has been tracked.
        // Do not call DbSet.Update to avoid EF marking the entire aggregate graph as Modified.
        // _context.Invoices.Update(invoice);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _context.Invoices.Remove(invoice);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return false;

        var normalized = invoiceNumber.Trim();

        return await _context.Invoices
            .AnyAsync(x => x.InvoiceNumber == normalized, cancellationToken);
    }

    private static IQueryable<Invoice> ApplyListFilter(IQueryable<Invoice> query, InvoiceListFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var normalized = filter.Keyword.Trim();

            query = query.Where(x =>
                x.InvoiceNumber.Contains(normalized) ||
                (x.Vendor != null && x.Vendor.Name.Contains(normalized)));
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

        return query;
    }

    private static IQueryable<Invoice> ApplyDashboardFilter(IQueryable<Invoice> query, DashboardFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.VendorId.HasValue)
            query = query.Where(x => x.VendorId == filter.VendorId.Value);

        if (filter.IssueDateFrom.HasValue)
            query = query.Where(x => x.IssueDate >= filter.IssueDateFrom.Value);

        if (filter.IssueDateTo.HasValue)
            query = query.Where(x => x.IssueDate <= filter.IssueDateTo.Value);

        return query;
    }

    private static IOrderedQueryable<Invoice> ApplyListSorting(
        IQueryable<Invoice> query,
        InvoiceListFilterDto filter)
    {
        var sortBy = filter.SortBy?.Trim();

        return sortBy switch
        {
            InvoiceListSortFields.CreatedAt => filter.SortDescending
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt),
            InvoiceListSortFields.InvoiceNumber => filter.SortDescending
                ? query.OrderByDescending(x => x.InvoiceNumber)
                : query.OrderBy(x => x.InvoiceNumber),
            InvoiceListSortFields.TotalAmount => filter.SortDescending
                ? query.OrderByDescending(x => x.DeclaredTotalAmount.Amount)
                : query.OrderBy(x => x.DeclaredTotalAmount.Amount),
            _ => filter.SortDescending
                ? query.OrderByDescending(x => x.IssueDate).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.IssueDate).ThenByDescending(x => x.CreatedAt)
        };
    }
}
