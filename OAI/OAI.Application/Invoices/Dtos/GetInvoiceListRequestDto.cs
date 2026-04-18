namespace OAI.Application.Invoices.Dtos;

public sealed record GetInvoiceListRequestDto
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Keyword { get; init; }
}