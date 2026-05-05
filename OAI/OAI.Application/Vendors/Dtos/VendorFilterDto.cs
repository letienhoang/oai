namespace OAI.Application.Vendors.Dtos;

public sealed record VendorFilterDto
{
    public string? Keyword { get; init; }
    public string? SortBy { get; init; } = VendorSortFields.Name;
    public bool SortDescending { get; init; }
}
