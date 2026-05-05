using OAI.Application.Dashboard.Dtos;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Enums;

namespace OAI.Application.Abstractions.Persistence;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        InvoiceListFilterDto filter,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(InvoiceListFilterDto filter, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? keyword = null, CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(
        InvoiceStatus status,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(
        InvoiceStatus status,
        CancellationToken cancellationToken = default);
    Task<int> CountWithValidationIssuesAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default);
    Task<int> CountWithValidationIssuesAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetRecentAsync(
        int take,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task DeleteAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task<bool> ExistsByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
}
