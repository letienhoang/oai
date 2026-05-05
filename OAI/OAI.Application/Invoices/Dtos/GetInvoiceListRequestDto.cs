namespace OAI.Application.Invoices.Dtos;

public sealed record GetInvoiceListRequestDto
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public InvoiceListFilterDto Filter { get; init; } = new();

    public string? Keyword
    {
        get => Filter.Keyword;
        init => Filter = Filter with { Keyword = value };
    }
}
