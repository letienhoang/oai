using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Mappings;
using OAI.Domain.Entities;
using OAI.Domain.Exceptions;
using OAI.Domain.ValueObjects;

namespace OAI.Application.UseCases.Invoices;

public sealed class CreateInvoiceUseCase : ICreateInvoiceUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateInvoiceUseCase> _logger;

    public CreateInvoiceUseCase(
        IInvoiceRepository invoiceRepository,
        IVendorRepository vendorRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateInvoiceUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InvoiceDetailDto> ExecuteAsync(
        InvoiceCreateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceNumber"] = request.InvoiceNumber,
            ["VendorId"] = request.VendorId
        });

        _logger.LogInformation("Start creating invoice {InvoiceNumber}", request.InvoiceNumber);

        ValidateRequest(request);

        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
        if (vendor is null)
        {
            _logger.LogWarning("Cannot create invoice because vendor {VendorId} was not found", request.VendorId);
            throw new DomainException(
                message: $"Vendor '{request.VendorId}' was not found.",
                code: InvoiceDomainErrorCodes.VendorNotFound,
                parameters: new Dictionary<string, string>
                {
                    ["VendorId"] = request.VendorId.ToString()
                });
        }

        var normalizedInvoiceNumber = request.InvoiceNumber.Trim();

        if (await _invoiceRepository.ExistsByInvoiceNumberAsync(normalizedInvoiceNumber, cancellationToken))
        {
            _logger.LogWarning("Cannot create invoice because invoice number {InvoiceNumber} already exists", normalizedInvoiceNumber);
            throw new DomainException(
                message: $"Invoice number '{normalizedInvoiceNumber}' already exists.",
                code: InvoiceDomainErrorCodes.InvoiceNumberAlreadyExists,
                parameters: new Dictionary<string, string>
                {
                    ["InvoiceNumber"] = normalizedInvoiceNumber
                });
        }

        var currency = request.Currency.Trim().ToUpperInvariant();

        var invoice = new Invoice(
            request.VendorId,
            normalizedInvoiceNumber,
            request.IssueDate,
            currency,
            new Money(request.DeclaredSubtotal, currency),
            new Money(request.DeclaredTaxAmount, currency),
            new Money(request.DeclaredTotalAmount, currency),
            request.DueDate,
            request.SourceFileName,
            request.SourceFilePath);

        invoice.AssignVendor(vendor);

        foreach (var line in request.LineItems.OrderBy(x => x.LineNo))
        {
            var lineItem = new InvoiceLineItem(
                invoice.Id,
                line.LineNo,
                line.Description,
                line.Quantity,
                new Money(line.UnitPrice, currency),
                line.TaxRate);

            invoice.AddLineItem(lineItem);
        }
        
        if (!string.IsNullOrWhiteSpace(request.ExtractionEngineName))
        {
            var extractionResult = new InvoiceExtractionResult(
                invoice.Id,
                request.ExtractionEngineName,
                request.ExtractionConfidenceScore ?? 0m,
                attemptNo: 1,
                isSuccessful: true,
                rawText: request.ExtractionRawText,
                structuredJson: request.ExtractionStructuredJson);

            invoice.AddExtractionResult(extractionResult);
        }

        var issues = invoice.ValidateConsistency().ToList();
        foreach (var issue in issues)
        {
            invoice.AddValidationIssue(issue);
        }

        if (issues.Count > 0)
        {
            _logger.LogWarning(
                "Invoice {InvoiceNumber} created with {IssueCount} validation issue(s)",
                normalizedInvoiceNumber,
                issues.Count);
        }

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invoice created successfully. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
            invoice.Id,
            invoice.InvoiceNumber);

        return invoice.ToDetailDto();
    }

    private static void ValidateRequest(InvoiceCreateRequestDto request)
    {
        if (request.VendorId == Guid.Empty)
            throw new DomainException(
                message: "Vendor is required.",
                code: InvoiceDomainErrorCodes.VendorRequired);

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
            throw new DomainException(
                message: "Invoice number is required.",
                code: InvoiceDomainErrorCodes.InvoiceNumberRequired);

        if (string.IsNullOrWhiteSpace(request.Currency))
            throw new DomainException(
                message: "Currency is required.",
                code: InvoiceDomainErrorCodes.CurrencyRequired);

        if (request.LineItems is null || request.LineItems.Count == 0)
            throw new DomainException(
                message: "At least one line item is required.",
                code: InvoiceDomainErrorCodes.LineItemsRequired);
    }
}
