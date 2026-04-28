using OAI.Application.Invoices.Dtos.ExtractionComparison;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface ICompareInvoiceExtractionUseCase
{
    Task<InvoiceExtractionComparisonDto> ExecuteAsync(
        CompareInvoiceExtractionRequestDto request,
        CancellationToken cancellationToken = default);
}