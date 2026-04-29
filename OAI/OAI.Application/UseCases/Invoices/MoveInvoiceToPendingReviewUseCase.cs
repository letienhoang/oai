using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class MoveInvoiceToPendingReviewUseCase : IMoveInvoiceToPendingReviewUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoveInvoiceToPendingReviewUseCase> _logger;

    public MoveInvoiceToPendingReviewUseCase(
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<MoveInvoiceToPendingReviewUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MoveInvoiceToPendingReviewResultDto> ExecuteAsync(
        MoveInvoiceToPendingReviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvoiceId == Guid.Empty)
            throw new DomainException("InvoiceId is required.");

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceId"] = request.InvoiceId
        });

        _logger.LogInformation(
            "Start moving invoice {InvoiceId} to pending review",
            request.InvoiceId);

        var invoice = await _invoiceRepository.GetByIdAsync(
            request.InvoiceId,
            cancellationToken);

        if (invoice is null)
        {
            _logger.LogWarning(
                "Cannot move invoice to pending review because invoice {InvoiceId} was not found",
                request.InvoiceId);

            throw new DomainException($"Invoice '{request.InvoiceId}' was not found.");
        }

        invoice.MoveToPendingReview();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invoice moved to pending review successfully. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
            invoice.Id,
            invoice.InvoiceNumber);

        return new MoveInvoiceToPendingReviewResultDto
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status.ToString(),
            Message = "Invoice moved to pending review successfully."
        };
    }
}