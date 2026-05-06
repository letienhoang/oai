using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Vendors;
using OAI.Application.Vendors.Dtos;

namespace OAI.Application.UseCases.Vendors;

public sealed class GetVendorOptionsUseCase : IGetVendorOptionsUseCase
{
    private readonly IVendorRepository _vendorRepository;

    public GetVendorOptionsUseCase(IVendorRepository vendorRepository)
    {
        _vendorRepository = vendorRepository;
    }

    public async Task<IReadOnlyList<VendorOptionDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var vendors = await _vendorRepository.GetAllAsync(cancellationToken);

        return vendors
            .OrderBy(vendor => vendor.Name)
            .Select(vendor => new VendorOptionDto
            {
                VendorId = vendor.Id,
                Name = vendor.Name
            })
            .ToList();
    }
}
