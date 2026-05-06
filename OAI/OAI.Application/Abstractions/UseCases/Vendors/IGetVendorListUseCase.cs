using OAI.Application.Common;
using OAI.Application.Vendors.Dtos;

namespace OAI.Application.Abstractions.UseCases.Vendors;

public interface IGetVendorListUseCase
{
    Task<PagedResultDto<VendorListItemDto>> ExecuteAsync(
        GetVendorListRequestDto request,
        CancellationToken cancellationToken = default);
}
