using OAI.Domain.Enums;

namespace OAI.Application.Invoices.Dtos;

public sealed record InvoiceListFilterDto
{
    public string? Keyword { get; init; }
    public InvoiceStatus? Status { get; init; }
    public Guid? VendorId { get; init; }
    public DateOnly? IssueDateFrom { get; init; }
    public DateOnly? IssueDateTo { get; init; }
    public decimal? TotalAmountFrom { get; init; }
    public decimal? TotalAmountTo { get; init; }
    public bool? HasOpenValidationIssues { get; init; }
    public string? SortBy { get; init; } = "IssueDate";
    public bool SortDescending { get; init; } = true;
}
