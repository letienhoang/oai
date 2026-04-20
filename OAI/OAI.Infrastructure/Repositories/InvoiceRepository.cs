using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
using OAI.Domain.Entities;
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
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Invoice> query = _context.Invoices;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim();

            query = query.Where(x =>
                x.InvoiceNumber.Contains(normalized) ||
                (x.Vendor != null && x.Vendor.Name.Contains(normalized)));
        }

        query = query
            .AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.LineItems);

        return await query
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim();

            query = query.Where(x =>
                x.InvoiceNumber.Contains(normalized) ||
                (x.Vendor != null && x.Vendor.Name.Contains(normalized)));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _context.Invoices.Update(invoice);
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
}