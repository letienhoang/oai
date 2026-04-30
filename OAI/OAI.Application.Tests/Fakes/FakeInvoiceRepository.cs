using OAI.Application.Abstractions.Persistence;
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
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Invoice> query = _invoices;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.InvoiceNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        var result = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Invoice>>(result);
    }

    public Task<int> CountAsync(
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Task.FromResult(_invoices.Count);

        return Task.FromResult(_invoices.Count(x =>
            x.InvoiceNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<int> CountByStatusAsync(
        InvoiceStatus status,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_invoices.Count(x => x.Status == status));
    }

    public Task<int> CountWithValidationIssuesAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_invoices.Count(x => x.ValidationIssues.Any()));
    }

    public Task<IReadOnlyList<Invoice>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var result = _invoices
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Invoice>>(result);
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
}