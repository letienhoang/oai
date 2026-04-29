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
            throw new DomainException("InvoiceId is required.");

        if (request.VendorId == Guid.Empty)
            throw new DomainException("Vendor is required.");

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
            throw new DomainException("Invoice number is required.");

        if (string.IsNullOrWhiteSpace(request.Currency))
            throw new DomainException("Currency is required.");

        if (request.LineItems.Count == 0)
            throw new DomainException("At least one line item is required.");

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
            throw new DomainException($"Invoice '{request.InvoiceId}' was not found.");
        }

        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
        if (vendor is null)
        {
            _logger.LogWarning("Cannot update invoice because vendor {VendorId} was not found", request.VendorId);
            throw new DomainException($"Vendor '{request.VendorId}' was not found.");
        }

        var normalizedInvoiceNumber = request.InvoiceNumber.Trim();
        var existing = await _invoiceRepository.GetByInvoiceNumberAsync(normalizedInvoiceNumber, cancellationToken);

        if (existing is not null && existing.Id != invoice.Id)
        {
            _logger.LogWarning(
                "Cannot update invoice because invoice number {InvoiceNumber} already exists",
                normalizedInvoiceNumber);

            throw new DomainException($"Invoice number '{normalizedInvoiceNumber}' already exists.");
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

                    throw new DomainException($"Invoice line item '{line.InvoiceLineItemId.Value}' was not found.");
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
        invoice.MarkAsPendingReview();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invoice updated successfully. InvoiceId: {InvoiceId}, ValidationIssueCount: {IssueCount}",
            invoice.Id,
            issues.Count);

        return invoice.ToDetailDto();
    }
}
