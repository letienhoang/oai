using OAI.Application.Invoices.Dtos.ExtractionComparison;

namespace OAI.Application.Abstractions.Services;

public interface IInvoiceExtractionComparisonService
{
    Task<InvoiceExtractionComparisonDto> CompareAsync(
        Guid invoiceId,
        string invoiceNumber,
        string rawText,
        CancellationToken cancellationToken = default);
}