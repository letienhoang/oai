namespace OAI.Application.Vendors.Dtos;

public sealed record VendorListItemDto
{
    public Guid VendorId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? TaxNumber { get; init; }
    public string? Address { get; init; }
    public string? Email { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
