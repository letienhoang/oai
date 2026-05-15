using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IInvoiceExtractionService
{
    Task<ExtractedInvoiceDto?> ExtractFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ExtractedInvoiceDto?> ExtractFromTextAsync(
        string rawText,
        string sourceName = "raw-text",
        decimal confidenceScore = 1.0m,
        string engineName = "RawText",
        CancellationToken cancellationToken = default);
}
