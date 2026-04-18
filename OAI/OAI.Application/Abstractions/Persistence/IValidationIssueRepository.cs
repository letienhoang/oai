using OAI.Domain.Entities;

namespace OAI.Application.Abstractions.Persistence;

public interface IValidationIssueRepository
{
    Task<IReadOnlyList<ValidationIssue>> GetByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ValidationIssue issue, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ValidationIssue> issues, CancellationToken cancellationToken = default);
    Task MarkResolvedAsync(Guid validationIssueId, CancellationToken cancellationToken = default);
}