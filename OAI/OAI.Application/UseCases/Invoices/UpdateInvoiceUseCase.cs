using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Mappings;
using OAI.Domain.Entities;
using OAI.Domain.Exceptions;
using OAI.Domain.ValueObjects;

namespace OAI.Application.UseCases.Invoices;

public sealed class UpdateInvoiceUseCase : IUpdateInvoiceUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateInvoiceUseCase> _logger;

    public UpdateInvoiceUseCase(
        IInvoiceRepository invoiceRepository,
        IVendorRepository vendorRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateInvoiceUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InvoiceDetailDto> ExecuteAsync(
        InvoiceUpdateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvoiceId == Guid.Empty)
            throw InvoiceDomainExceptionFactory.InvoiceIdRequired();

        if (request.VendorId == Guid.Empty)
            throw InvoiceDomainExceptionFactory.VendorRequired();

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
            throw InvoiceDomainExceptionFactory.InvoiceNumberRequired();

        if (string.IsNullOrWhiteSpace(request.Currency))
            throw InvoiceDomainExceptionFactory.CurrencyRequired();

        if (request.LineItems.Count == 0)
            throw InvoiceDomainExceptionFactory.LineItemsRequired();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceId"] = request.InvoiceId,
            ["InvoiceNumber"] = request.InvoiceNumber
        });

        _logger.LogInformation("Start updating invoice {InvoiceId}", request.InvoiceId);

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogWarning("Cannot update invoice because invoice {InvoiceId} was not found", request.InvoiceId);
            throw InvoiceDomainExceptionFactory.InvoiceNotFound(request.InvoiceId);
        }

        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
        if (vendor is null)
        {
            _logger.LogWarning("Cannot update invoice because vendor {VendorId} was not found", request.VendorId);
            throw InvoiceDomainExceptionFactory.VendorNotFound(request.VendorId);
        }

        var normalizedInvoiceNumber = request.InvoiceNumber.Trim();
        var existing = await _invoiceRepository.GetByInvoiceNumberAsync(normalizedInvoiceNumber, cancellationToken);

        if (existing is not null && existing.Id != invoice.Id)
        {
            _logger.LogWarning(
                "Cannot update invoice because invoice number {InvoiceNumber} already exists",
                normalizedInvoiceNumber);

            throw InvoiceDomainExceptionFactory.InvoiceNumberAlreadyExists(normalizedInvoiceNumber);
        }

        var currency = request.Currency.Trim().ToUpperInvariant();

        invoice.UpdateHeader(
            request.VendorId,
            normalizedInvoiceNumber,
            request.IssueDate,
            request.DueDate,
            currency);

        invoice.AssignVendor(vendor);

        invoice.UpdateDeclaredAmounts(
            new Money(request.DeclaredSubtotal, currency),
            new Money(request.DeclaredTaxAmount, currency),
            new Money(request.DeclaredTotalAmount, currency));

        var existingLineItems = invoice.LineItems
            .ToDictionary(x => x.Id);

        var existingIdsBeforeUpdate = existingLineItems.Keys.ToHashSet();

        var requestedExistingIds = new HashSet<Guid>();

        foreach (var line in request.LineItems.OrderBy(x => x.LineNo))
        {
            if (line.InvoiceLineItemId.HasValue && line.InvoiceLineItemId.Value != Guid.Empty)
            {
                if (!existingLineItems.TryGetValue(line.InvoiceLineItemId.Value, out var existingLineItem))
                {
                    _logger.LogWarning(
                        "Cannot update invoice because line item {InvoiceLineItemId} was not found on invoice {InvoiceId}",
                        line.InvoiceLineItemId.Value,
                        invoice.Id);

                    throw InvoiceDomainExceptionFactory.LineItemNotFound(line.InvoiceLineItemId.Value);
                }

                existingLineItem.Update(
                    line.LineNo,
                    line.Description,
                    line.Quantity,
                    new Money(line.UnitPrice, currency),
                    line.TaxRate);

                requestedExistingIds.Add(existingLineItem.Id);
            }
            else
            {
                var newLineItem = new InvoiceLineItem(
                    invoice.Id,
                    line.LineNo,
                    line.Description,
                    line.Quantity,
                    new Money(line.UnitPrice, currency),
                    line.TaxRate);

                invoice.AddLineItem(newLineItem);
            }
        }

        var removedLineItemIds = existingIdsBeforeUpdate
            .Where(id => !requestedExistingIds.Contains(id))
            .ToList();

        foreach (var removedLineItemId in removedLineItemIds)
        {
            invoice.RemoveLineItem(removedLineItemId);
        }

        var issues = invoice.ValidateConsistency().ToList();
        invoice.ReplaceValidationIssues(issues);
        invoice.MoveToPendingReview();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invoice updated successfully. InvoiceId: {InvoiceId}, ValidationIssueCount: {IssueCount}",
            invoice.Id,
            issues.Count);

        return invoice.ToDetailDto();
    }
}
