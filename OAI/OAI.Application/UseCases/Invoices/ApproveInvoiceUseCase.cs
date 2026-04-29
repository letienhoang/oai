using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class ApproveInvoiceUseCase : IApproveInvoiceUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveInvoiceUseCase> _logger;

    public ApproveInvoiceUseCase(
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<ApproveInvoiceUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApproveInvoiceResultDto> ExecuteAsync(
        ApproveInvoiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvoiceId == Guid.Empty)
            throw new DomainException("InvoiceId is required.");

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceId"] = request.InvoiceId
        });

        _logger.LogInformation("Start approving invoice {InvoiceId}", request.InvoiceId);

        var invoice = await _invoiceRepository.GetByIdAsync(
            request.InvoiceId,
            cancellationToken);

        if (invoice is null)
        {
            _logger.LogWarning(
                "Cannot approve invoice because invoice {InvoiceId} was not found",
                request.InvoiceId);

            throw new DomainException($"Invoice '{request.InvoiceId}' was not found.");
        }

        invoice.Approve();
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invoice approved successfully. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
            invoice.Id,
            invoice.InvoiceNumber);

        return new ApproveInvoiceResultDto
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status.ToString(),
            Message = "Invoice approved successfully."
        };
    }
}