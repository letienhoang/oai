using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Mappings;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class GetInvoiceDetailUseCase : IGetInvoiceDetailUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<GetInvoiceDetailUseCase> _logger;

    public GetInvoiceDetailUseCase(IInvoiceRepository invoiceRepository, ILogger<GetInvoiceDetailUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    public async Task<InvoiceDetailDto> ExecuteAsync(
        GetInvoiceDetailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting invoice detail. InvoiceId: {InvoiceId}", request.InvoiceId);
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvoiceId == Guid.Empty)
            throw new DomainException("InvoiceId is required.");

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogWarning("Invoice detail not found. InvoiceId: {InvoiceId}", request.InvoiceId);
            throw new DomainException($"Invoice '{request.InvoiceId}' was not found.");
        }

        return invoice.ToDetailDto();
    }
}