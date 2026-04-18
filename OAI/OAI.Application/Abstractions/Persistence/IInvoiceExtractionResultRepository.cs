using OAI.Domain.Entities;

namespace OAI.Application.Abstractions.Persistence;

public interface IInvoiceExtractionResultRepository
{
    Task<IReadOnlyList<InvoiceExtractionResult>> GetByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(InvoiceExtractionResult result, CancellationToken cancellationToken = default);
}