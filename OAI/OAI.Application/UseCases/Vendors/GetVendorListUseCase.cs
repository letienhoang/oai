using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Vendors;
using OAI.Application.Common;
using OAI.Application.Vendors.Dtos;

namespace OAI.Application.UseCases.Vendors;

public sealed class GetVendorListUseCase : IGetVendorListUseCase
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly IVendorRepository _vendorRepository;
    private readonly ILogger<GetVendorListUseCase> _logger;

    public GetVendorListUseCase(
        IVendorRepository vendorRepository,
        ILogger<GetVendorListUseCase> logger)
    {
        _vendorRepository = vendorRepository;
        _logger = logger;
    }

    public async Task<PagedResultDto<VendorListItemDto>> ExecuteAsync(
        GetVendorListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageNumber = request.PageNumber <= 0 ? DefaultPageNumber : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var filter = request.Filter ?? new VendorFilterDto();

        var totalItems = await _vendorRepository.CountAsync(filter, cancellationToken);
        var vendors = await _vendorRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter,
            cancellationToken);

        _logger.LogInformation(
            "Getting vendor list. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}",
            pageNumber,
            pageSize,
            filter.Keyword);

        return new PagedResultDto<VendorListItemDto>
        {
            Items = vendors.Select(ToListItemDto).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    private static VendorListItemDto ToListItemDto(Domain.Entities.Vendor vendor)
    {
        return new VendorListItemDto
        {
            VendorId = vendor.Id,
            Name = vendor.Name,
            TaxNumber = vendor.TaxNumber,
            Address = vendor.Address,
            Email = vendor.Email,
            CreatedAt = vendor.CreatedAt
        };
    }
}
