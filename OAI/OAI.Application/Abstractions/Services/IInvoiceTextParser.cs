using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface IInvoiceTextParser
{
    ExtractedInvoiceDto? Parse(
        string rawText,
        string sourceFileName,
        decimal confidenceScore,
        string engineName);
}