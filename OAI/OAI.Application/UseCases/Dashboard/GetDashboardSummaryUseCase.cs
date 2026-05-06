using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Dashboard;
using OAI.Application.Dashboard.Dtos;
using OAI.Application.Validation;
using OAI.Domain.Enums;

namespace OAI.Application.UseCases.Dashboard;

public sealed class GetDashboardSummaryUseCase : IGetDashboardSummaryUseCase
{
    private const int DefaultRecentItemCount = 5;
    private const int MaxRecentItemCount = 20;

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
        return await ExecuteAsync(new GetDashboardSummaryRequestDto(), cancellationToken);
    }

    public async Task<DashboardSummaryDto> ExecuteAsync(
        GetDashboardSummaryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting dashboard summary.");
        ArgumentNullException.ThrowIfNull(request);

        var filter = request.Filter;
        var recentInvoiceCount = ClampRecentCount(filter.RecentInvoiceCount);
        var recentValidationIssueCount = ClampRecentCount(filter.RecentValidationIssueCount);

        var draftInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.Draft,
            filter,
            cancellationToken);

        var pendingReviewInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.PendingReview,
            filter,
            cancellationToken);

        var approvedInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.Approved,
            filter,
            cancellationToken);

        var rejectedInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.Rejected,
            filter,
            cancellationToken);

        var exportedInvoices = await _invoiceRepository.CountByStatusAsync(
            InvoiceStatus.Exported,
            filter,
            cancellationToken);

        var totalInvoices = draftInvoices
            + pendingReviewInvoices
            + approvedInvoices
            + rejectedInvoices
            + exportedInvoices;

        var invoicesWithIssues = await _invoiceRepository.CountWithValidationIssuesAsync(
            filter,
            cancellationToken);

        var openIssues = await _validationIssueRepository.CountOpenAsync(
            filter,
            cancellationToken);

        var resolvedIssues = await _validationIssueRepository.CountResolvedAsync(
            filter,
            cancellationToken);

        var recentInvoicesRaw = await _invoiceRepository.GetRecentAsync(
            recentInvoiceCount,
            filter,
            cancellationToken);

        var recentIssuesRaw = await _validationIssueRepository.GetRecentAsync(
            recentValidationIssueCount,
            filter,
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

    private static int ClampRecentCount(int count)
    {
        if (count <= 0)
            return DefaultRecentItemCount;

        return Math.Min(count, MaxRecentItemCount);
    }
}
