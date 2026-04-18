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

    public CreateInvoiceUseCase(
        IInvoiceRepository invoiceRepository,
        IVendorRepository vendorRepository,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceDetailDto> ExecuteAsync(
        InvoiceCreateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var vendor = await _vendorRepository.GetByIdAsync(request.VendorId, cancellationToken);
        if (vendor is null)
            throw new DomainException($"Vendor '{request.VendorId}' was not found.");

        var normalizedInvoiceNumber = request.InvoiceNumber.Trim();

        if (await _invoiceRepository.ExistsByInvoiceNumberAsync(normalizedInvoiceNumber, cancellationToken))
            throw new DomainException($"Invoice number '{normalizedInvoiceNumber}' already exists.");

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

        var issues = invoice.ValidateConsistency();
        foreach (var issue in issues)
        {
            invoice.AddValidationIssue(issue);
        }

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return invoice.ToDetailDto();
    }

    private static void ValidateRequest(InvoiceCreateRequestDto request)
    {
        if (request.VendorId == Guid.Empty)
            throw new DomainException("Vendor is required.");

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
            throw new DomainException("Invoice number is required.");

        if (string.IsNullOrWhiteSpace(request.Currency))
            throw new DomainException("Currency is required.");

        if (request.LineItems is null || request.LineItems.Count == 0)
            throw new DomainException("At least one line item is required.");
    }
}