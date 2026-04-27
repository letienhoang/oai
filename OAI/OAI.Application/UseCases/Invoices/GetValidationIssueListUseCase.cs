using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Common;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class GetValidationIssueListUseCase : IGetValidationIssueListUseCase
{
    private readonly IValidationIssueRepository _validationIssueRepository;
    private readonly ILogger<GetValidationIssueListUseCase> _logger;

    public GetValidationIssueListUseCase(
        IValidationIssueRepository validationIssueRepository,
        ILogger<GetValidationIssueListUseCase> logger)
    {
        _validationIssueRepository = validationIssueRepository;
        _logger = logger;
    }

    public async Task<PagedResultDto<ValidationIssueListItemDto>> ExecuteAsync(
        GetValidationIssueListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PageNumber <= 0)
            throw new DomainException("PageNumber must be greater than zero.");

        if (request.PageSize <= 0)
            throw new DomainException("PageSize must be greater than zero.");

        _logger.LogInformation(
            "Getting validation issue list. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}, Severity: {Severity}, IsResolved: {IsResolved}",
            request.PageNumber,
            request.PageSize,
            request.Keyword,
            request.Severity,
            request.IsResolved);

        var totalItems = await _validationIssueRepository.CountAsync(
            request.Keyword,
            request.Severity,
            request.IsResolved,
            cancellationToken);

        var issues = await _validationIssueRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Keyword,
            request.Severity,
            request.IsResolved,
            cancellationToken);

        var items = issues
            .Select(issue => new ValidationIssueListItemDto
            {
                ValidationIssueId = issue.Id,
                InvoiceId = issue.InvoiceId,
                InvoiceNumber = issue.Invoice?.InvoiceNumber ?? string.Empty,
                VendorName = issue.Invoice?.Vendor?.Name ?? string.Empty,
                FieldName = issue.FieldName,
                RuleCode = issue.RuleCode,
                Message = issue.Message,
                Severity = issue.Severity.ToString(),
                IsResolved = issue.IsResolved,
                DetectedAt = issue.DetectedAt
            })
            .ToList();

        return new PagedResultDto<ValidationIssueListItemDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = totalItems
        };
    }
}