using OAI.Domain.Entities;

namespace OAI.Application.Abstractions.Services;

public interface IInvoiceValidationService
{
    Task<IReadOnlyList<ValidationIssue>> ValidateAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default);
}