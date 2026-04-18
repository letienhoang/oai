using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IInvoiceExtractionService
{
    Task<InvoiceDetailDto?> ExtractAsync(
        Guid invoiceId,
        string rawText,
        CancellationToken cancellationToken = default);

    Task<InvoiceDetailDto?> ExtractFromFileAsync(
        Guid invoiceId,
        string filePath,
        CancellationToken cancellationToken = default);
}