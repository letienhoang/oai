namespace OAI.Application.Dashboard.Dtos;

public sealed record RecentInvoiceDto
{
    public Guid InvoiceId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public string VendorName { get; init; } = string.Empty;

    public DateOnly IssueDate { get; init; }

    public decimal TotalAmount { get; init; }

    public string Currency { get; init; } = "VND";

    public string Status { get; init; } = string.Empty;
}