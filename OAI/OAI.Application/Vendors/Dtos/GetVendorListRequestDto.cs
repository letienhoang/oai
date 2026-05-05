namespace OAI.Application.Vendors.Dtos;

public sealed record GetVendorListRequestDto
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public VendorFilterDto Filter { get; init; } = new();

    public string? Keyword
    {
        get => Filter.Keyword;
        init => Filter = Filter with { Keyword = value };
    }
}
