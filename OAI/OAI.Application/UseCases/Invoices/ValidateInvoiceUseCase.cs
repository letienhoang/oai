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

    public ValidateInvoiceUseCase(
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceValidationResultDto> ExecuteAsync(
        ValidateInvoiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.InvoiceId == Guid.Empty)
            throw new DomainException("InvoiceId is required.");

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
            throw new DomainException($"Invoice '{request.InvoiceId}' was not found.");

        var issues = invoice.ValidateConsistency(request.Tolerance).ToList();

        invoice.ReplaceValidationIssues(issues);

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
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

        var hasErrors = issues.Any(x => x.Severity == ValidationSeverity.Error);

        return new InvoiceValidationResultDto
        {
            InvoiceId = invoice.Id,
            IsValid = !hasErrors,
            ErrorCount = issues.Count(x => x.Severity == ValidationSeverity.Error),
            WarningCount = issues.Count(x => x.Severity == ValidationSeverity.Warning),
            Issues = dtoIssues
        };
    }
}