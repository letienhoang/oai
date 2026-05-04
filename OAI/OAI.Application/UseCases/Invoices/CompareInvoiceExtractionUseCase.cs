using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos.ExtractionComparison;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class CompareInvoiceExtractionUseCase : ICompareInvoiceExtractionUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoiceExtractionComparisonService _comparisonService;
    private readonly ILogger<CompareInvoiceExtractionUseCase> _logger;

    public CompareInvoiceExtractionUseCase(
        IInvoiceRepository invoiceRepository,
        IInvoiceExtractionComparisonService comparisonService,
        ILogger<CompareInvoiceExtractionUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _comparisonService = comparisonService;
        _logger = logger;
    }

    public async Task<InvoiceExtractionComparisonDto> ExecuteAsync(
        CompareInvoiceExtractionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.InvoiceId == Guid.Empty)
            throw InvoiceDomainExceptionFactory.InvoiceIdRequired();

        _logger.LogInformation(
            "Comparing extraction results. InvoiceId: {InvoiceId}",
            request.InvoiceId);

        var invoice = await _invoiceRepository.GetByIdAsync(
            request.InvoiceId,
            cancellationToken);

        if (invoice is null)
            throw InvoiceDomainExceptionFactory.InvoiceNotFound(request.InvoiceId);

        var latestExtraction = invoice.ExtractionResults
            .OrderByDescending(x => x.ExtractedAt)
            .FirstOrDefault();

        if (latestExtraction is null || string.IsNullOrWhiteSpace(latestExtraction.RawText))
        {
            throw new DomainException(
                "Cannot compare extraction because OCR raw text was not found.");
        }

        return await _comparisonService.CompareAsync(
            invoice.Id,
            invoice.InvoiceNumber,
            latestExtraction.RawText,
            cancellationToken);
    }
}
