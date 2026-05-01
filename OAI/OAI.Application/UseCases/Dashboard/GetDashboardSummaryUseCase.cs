using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Dashboard;
using OAI.Application.Dashboard.Dtos;
using OAI.Application.Validation;
using OAI.Domain.Enums;

namespace OAI.Application.UseCases.Dashboard;

public sealed class GetDashboardSummaryUseCase : IGetDashboardSummaryUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IValidationIssueRepository _validationIssueRepository;
    private readonly ILogger<GetDashboardSummaryUseCase> _logger;

    public GetDashboardSummaryUseCase(
        IInvoiceRepository invoiceRepository,
        IValidationIssueRepository validationIssueRepository,
        ILogger<GetDashboardSummaryUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _validationIssueRepository = validationIssueRepository;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting dashboard summary.");

        var totalInvoices = await _invoiceRepository.CountAsync(
            cancellationToken: cancellationToken);

        var draftInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.Draft,
            cancellationToken);

        var pendingReviewInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.PendingReview,
            cancellationToken);

        var approvedInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.Approved,
            cancellationToken);

        var rejectedInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.Rejected,
            cancellationToken);

        var exportedInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.Exported,
            cancellationToken);

        var invoicesWithIssues = await _invoiceRepository.CountWithValidationIssuesAsync(
            cancellationToken);

        var openIssues = await _validationIssueRepository.CountOpenAsync(
            cancellationToken);

        var resolvedIssues = await _validationIssueRepository.CountResolvedAsync(
            cancellationToken);

        var recentInvoicesRaw = await _invoiceRepository.GetRecentAsync(
            5,
            cancellationToken);

        var recentIssuesRaw = await _validationIssueRepository.GetRecentAsync(
            5,
            cancellationToken);

        var recentInvoices = recentInvoicesRaw
            .Select(invoice => new RecentInvoiceDto
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                VendorName = invoice.Vendor?.Name ?? string.Empty,
                IssueDate = invoice.IssueDate,
                TotalAmount = invoice.DeclaredTotalAmount.Amount,
                Currency = invoice.Currency,
                Status = invoice.Status.ToString()
            })
            .ToList();

        var recentIssues = recentIssuesRaw
            .Select(issue => new RecentValidationIssueDto
            {
                ValidationIssueId = issue.Id,
                InvoiceId = issue.InvoiceId,
                InvoiceNumber = issue.Invoice?.InvoiceNumber ?? string.Empty,
                RuleCode = issue.RuleCode,
                FieldName = issue.FieldName,
                Message = issue.Message,
                MessageCode = ValidationIssueMessageMapper.GetMessageCode(issue),
                MessageParameters = ValidationIssueMessageMapper.GetMessageParameters(issue, issue.Invoice),
                Severity = issue.Severity.ToString(),
                IsResolved = issue.IsResolved,
                DetectedAt = issue.DetectedAt
            })
            .ToList();

        return new DashboardSummaryDto
        {
            TotalInvoices = totalInvoices,
            DraftInvoices = draftInvoices,
            PendingReviewInvoices = pendingReviewInvoices,
            ApprovedInvoices = approvedInvoices,
            RejectedInvoices = rejectedInvoices,
            ExportedInvoices = exportedInvoices,
            InvoicesWithValidationIssues = invoicesWithIssues,
            OpenValidationIssues = openIssues,
            ResolvedValidationIssues = resolvedIssues,
            TotalValidationIssues = openIssues + resolvedIssues,
            RecentInvoices = recentInvoices,
            RecentValidationIssues = recentIssues
        };
    }
}
