using OAI.Application.Vendors.Dtos;

namespace OAI.Application.Abstractions.UseCases.Vendors;

public interface IGetVendorOptionsUseCase
{
    Task<IReadOnlyList<VendorOptionDto>> ExecuteAsync(
        CancellationToken cancellationToken = default);
}
