namespace OAI.Application.Vendors.Dtos;

public sealed record VendorOptionDto
{
    public Guid VendorId { get; init; }
    public string Name { get; init; } = string.Empty;
}
