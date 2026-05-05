using OAI.Application.Dashboard.Dtos;
using OAI.Domain.Entities;

namespace OAI.Application.Abstractions.Persistence;

public interface IValidationIssueRepository
{
    Task<IReadOnlyList<ValidationIssue>> GetByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ValidationIssue>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null,
        string? severity = null,
        bool? isResolved = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        string? keyword = null,
        string? severity = null,
        bool? isResolved = null,
        CancellationToken cancellationToken = default);

    Task<int> CountOpenAsync(CancellationToken cancellationToken = default);

    Task<int> CountOpenAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<int> CountResolvedAsync(CancellationToken cancellationToken = default);

    Task<int> CountResolvedAsync(
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ValidationIssue>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ValidationIssue>> GetRecentAsync(
        int take,
        DashboardFilterDto filter,
        CancellationToken cancellationToken = default);

    Task AddAsync(ValidationIssue issue, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<ValidationIssue> issues, CancellationToken cancellationToken = default);

    Task MarkResolvedAsync(Guid validationIssueId, CancellationToken cancellationToken = default);
}
