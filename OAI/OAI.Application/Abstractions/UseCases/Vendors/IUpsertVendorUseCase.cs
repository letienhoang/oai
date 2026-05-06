using OAI.Application.Vendors.Dtos;

namespace OAI.Application.Abstractions.UseCases.Vendors;

public interface IUpsertVendorUseCase
{
    Task<VendorListItemDto> ExecuteAsync(
        VendorUpsertRequestDto request,
        CancellationToken cancellationToken = default);
}
