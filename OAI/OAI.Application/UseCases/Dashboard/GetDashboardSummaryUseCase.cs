using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Dashboard;
using OAI.Application.Dashboard.Dtos;
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

    public async Task<DashboardSummaryDto> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting dashboard summary.");

        var totalInvoicesTask = _invoiceRepository.CountAsync(cancellationToken: cancellationToken);

        var draftTask = _invoiceRepository.CountByStatusAsync(InvoiceStatus.Draft, cancellationToken);
        var pendingReviewTask = _invoiceRepository.CountByStatusAsync(InvoiceStatus.PendingReview, cancellationToken);
        var approvedTask = _invoiceRepository.CountByStatusAsync(InvoiceStatus.Approved, cancellationToken);
        var rejectedTask = _invoiceRepository.CountByStatusAsync(InvoiceStatus.Rejected, cancellationToken);
        var exportedTask = _invoiceRepository.CountByStatusAsync(InvoiceStatus.Exported, cancellationToken);

        var invoicesWithIssuesTask = _invoiceRepository.CountWithValidationIssuesAsync(cancellationToken);

        var openIssuesTask = _validationIssueRepository.CountOpenAsync(cancellationToken);
        var resolvedIssuesTask = _validationIssueRepository.CountResolvedAsync(cancellationToken);

        var recentInvoicesTask = _invoiceRepository.GetRecentAsync(5, cancellationToken);
        var recentIssuesTask = _validationIssueRepository.GetRecentAsync(5, cancellationToken);

        await Task.WhenAll(
            totalInvoicesTask,
            draftTask,
            pendingReviewTask,
            approvedTask,
            rejectedTask,
            exportedTask,
            invoicesWithIssuesTask,
            openIssuesTask,
            resolvedIssuesTask,
            recentInvoicesTask,
            recentIssuesTask);

        var openIssues = await openIssuesTask;
        var resolvedIssues = await resolvedIssuesTask;

        var recentInvoices = (await recentInvoicesTask)
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

        var recentIssues = (await recentIssuesTask)
            .Select(issue => new RecentValidationIssueDto
            {
                ValidationIssueId = issue.Id,
                InvoiceId = issue.InvoiceId,
                InvoiceNumber = issue.Invoice?.InvoiceNumber ?? string.Empty,
                RuleCode = issue.RuleCode,
                FieldName = issue.FieldName,
                Message = issue.Message,
                Severity = issue.Severity.ToString(),
                IsResolved = issue.IsResolved,
                DetectedAt = issue.DetectedAt
            })
            .ToList();

        return new DashboardSummaryDto
        {
            TotalInvoices = await totalInvoicesTask,
            DraftInvoices = await draftTask,
            PendingReviewInvoices = await pendingReviewTask,
            ApprovedInvoices = await approvedTask,
            RejectedInvoices = await rejectedTask,
            ExportedInvoices = await exportedTask,
            InvoicesWithValidationIssues = await invoicesWithIssuesTask,
            OpenValidationIssues = openIssues,
            ResolvedValidationIssues = resolvedIssues,
            TotalValidationIssues = openIssues + resolvedIssues,
            RecentInvoices = recentInvoices,
            RecentValidationIssues = recentIssues
        };
    }
}