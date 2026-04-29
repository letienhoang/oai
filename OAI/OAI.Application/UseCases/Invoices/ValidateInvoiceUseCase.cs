using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Enums;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class ValidateInvoiceUseCase : IValidateInvoiceUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ValidateInvoiceUseCase> _logger;

    public ValidateInvoiceUseCase(
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<ValidateInvoiceUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InvoiceValidationResultDto> ExecuteAsync(
        ValidateInvoiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvoiceId == Guid.Empty)
            throw new DomainException("InvoiceId is required.");

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceId"] = request.InvoiceId
        });

        _logger.LogInformation("Start validating invoice {InvoiceId}", request.InvoiceId);

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            _logger.LogWarning("Cannot validate invoice because invoice {InvoiceId} was not found", request.InvoiceId);
            throw new DomainException($"Invoice '{request.InvoiceId}' was not found.");
        }

        var issues = invoice.ValidateConsistency(request.Tolerance).ToList();

        invoice.ReplaceValidationIssues(issues);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dtoIssues = issues
            .Select(x => new ValidationIssueDto
            {
                ValidationIssueId = x.Id,
                FieldName = x.FieldName,
                RuleCode = x.RuleCode,
                Message = x.Message,
                Severity = x.Severity.ToString(),
                IsResolved = x.IsResolved,
                DetectedAt = x.DetectedAt,
                ResolvedAt = x.ResolvedAt
            })
            .ToList();

        var hasError = issues.Any(x => x.Severity == ValidationSeverity.Error);

        _logger.LogInformation(
            "Invoice validation completed. InvoiceId: {InvoiceId}, IsValid: {IsValid}, ErrorCount: {ErrorCount}, WarningCount: {WarningCount}",
            invoice.Id,
            !hasError,
            issues.Count(x => x.Severity == ValidationSeverity.Error),
            issues.Count(x => x.Severity == ValidationSeverity.Warning));

        return new InvoiceValidationResultDto
        {
            InvoiceId = invoice.Id,
            IsValid = !hasError,
            ErrorCount = issues.Count(x => x.Severity == ValidationSeverity.Error),
            WarningCount = issues.Count(x => x.Severity == ValidationSeverity.Warning),
            Issues = dtoIssues
        };
    }
}