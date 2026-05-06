namespace OAI.Application.Dashboard.Dtos;

public sealed record DashboardSummaryDto
{
    public int TotalInvoices { get; init; }

    public int DraftInvoices { get; init; }

    public int PendingReviewInvoices { get; init; }

    public int ApprovedInvoices { get; init; }

    public int RejectedInvoices { get; init; }

    public int ExportedInvoices { get; init; }

    public int InvoicesWithValidationIssues { get; init; }

    public int TotalValidationIssues { get; init; }

    public int OpenValidationIssues { get; init; }

    public int ResolvedValidationIssues { get; init; }

    public IReadOnlyList<RecentInvoiceDto> RecentInvoices { get; init; } = Array.Empty<RecentInvoiceDto>();

    public IReadOnlyList<RecentValidationIssueDto> RecentValidationIssues { get; init; } = Array.Empty<RecentValidationIssueDto>();
}