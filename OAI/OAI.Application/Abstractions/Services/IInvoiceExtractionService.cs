using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IInvoiceExtractionService
{
    Task<ExtractedInvoiceDto?> ExtractFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ExtractedInvoiceDto?> ExtractFromTextAsync(
        string rawText,
        CancellationToken cancellationToken = default);
}