using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class RejectInvoiceUseCase : IRejectInvoiceUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectInvoiceUseCase> _logger;

    public RejectInvoiceUseCase(
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<RejectInvoiceUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RejectInvoiceResultDto> ExecuteAsync(
        RejectInvoiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvoiceId == Guid.Empty)
            throw InvoiceDomainExceptionFactory.InvoiceIdRequired();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceId"] = request.InvoiceId
        });

        _logger.LogInformation("Start rejecting invoice {InvoiceId}", request.InvoiceId);

        var invoice = await _invoiceRepository.GetByIdAsync(
            request.InvoiceId,
            cancellationToken);

        if (invoice is null)
        {
            _logger.LogWarning(
                "Cannot reject invoice because invoice {InvoiceId} was not found",
                request.InvoiceId);

            throw InvoiceDomainExceptionFactory.InvoiceNotFound(request.InvoiceId);
        }

        invoice.Reject();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invoice rejected successfully. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
            invoice.Id,
            invoice.InvoiceNumber);

        return new RejectInvoiceResultDto
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status.ToString(),
            Message = "Invoice rejected successfully."
        };
    }
}
