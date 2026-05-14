using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;

namespace OAI.Infrastructure.Services;

public sealed class InvoiceExtractionService : IInvoiceExtractionService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IOcrService _ocrService;
    private readonly IInvoiceTextParser _invoiceTextParser;
    private readonly ILogger<InvoiceExtractionService> _logger;

    public InvoiceExtractionService(
        IFileStorageService fileStorageService,
        IOcrService ocrService,
        IInvoiceTextParser invoiceTextParser,
        ILogger<InvoiceExtractionService> logger)
    {
        _fileStorageService = fileStorageService;
        _ocrService = ocrService;
        _invoiceTextParser = invoiceTextParser;
        _logger = logger;
    }

    public async Task<ExtractedInvoiceDto?> ExtractFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        _logger.LogInformation("Start extracting invoice from file path {FilePath}", filePath);

        var stream = await _fileStorageService.OpenReadAsync(filePath, cancellationToken);
        if (stream is null)
        {
            _logger.LogWarning("Cannot extract invoice because file was not found at {FilePath}", filePath);
            return null;
        }

        await using (stream)
        {
            var fileName = Path.GetFileName(filePath);

            var ocrResult = await _ocrService.ExtractTextAsync(stream, fileName, cancellationToken);
            if (!ocrResult.IsSuccess || string.IsNullOrWhiteSpace(ocrResult.Text))
            {
                _logger.LogWarning(
                    "OCR failed or returned empty text for file {FileName}. Error: {ErrorMessage}",
                    fileName,
                    ocrResult.ErrorMessage);

                return null;
            }

            var extracted = await _invoiceTextParser.ParseAsync(
                ocrResult.Text,
                fileName,
                (decimal)ocrResult.Confidence,
                "Tesseract",
                cancellationToken);

            if (extracted is null)
            {
                _logger.LogWarning("Cannot parse invoice data from OCR text for file {FileName}", fileName);
                return null;
            }

            _logger.LogInformation(
                "Invoice extraction succeeded for file {FileName}. InvoiceNumber: {InvoiceNumber}, Confidence: {Confidence}",
                fileName,
                extracted.InvoiceNumber,
                extracted.ConfidenceScore);

            return extracted;
        }
    }

    public Task<ExtractedInvoiceDto?> ExtractFromTextAsync(
        string rawText,
        string sourceName = "raw-text",
        decimal confidenceScore = 1.0m,
        string engineName = "RawText",
        CancellationToken cancellationToken = default)
    {
        return _invoiceTextParser.ParseAsync(
            rawText,
            string.IsNullOrWhiteSpace(sourceName) ? "raw-text" : sourceName,
            confidenceScore,
            string.IsNullOrWhiteSpace(engineName) ? "RawText" : engineName,
            cancellationToken);
    }
}
