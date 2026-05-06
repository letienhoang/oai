namespace OAI.Application.Dashboard.Dtos;

public sealed record DashboardFilterDto
{
    public DateOnly? IssueDateFrom { get; init; }
    public DateOnly? IssueDateTo { get; init; }
    public Guid? VendorId { get; init; }
    public int RecentInvoiceCount { get; init; } = 5;
    public int RecentValidationIssueCount { get; init; } = 5;
}
