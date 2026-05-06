using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IInvoiceTextParser
{
    Task<ExtractedInvoiceDto?> ParseAsync(
        string rawText,
        string sourceFileName,
        decimal confidenceScore,
        string ocrEngineName,
        CancellationToken cancellationToken = default);
}