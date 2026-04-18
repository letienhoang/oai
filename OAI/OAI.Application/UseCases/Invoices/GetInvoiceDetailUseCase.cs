using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Mappings;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class GetInvoiceDetailUseCase : IGetInvoiceDetailUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceDetailUseCase(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<InvoiceDetailDto> ExecuteAsync(
        GetInvoiceDetailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvoiceId == Guid.Empty)
            throw new DomainException("InvoiceId is required.");

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
            throw new DomainException($"Invoice '{request.InvoiceId}' was not found.");

        return invoice.ToDetailDto();
    }
}