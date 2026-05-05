using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Vendors;
using OAI.Application.Vendors.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Vendors;

public sealed class UpsertVendorUseCase : IUpsertVendorUseCase
{
    private readonly IVendorRepository _vendorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpsertVendorUseCase> _logger;

    public UpsertVendorUseCase(
        IVendorRepository vendorRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpsertVendorUseCase> logger)
    {
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<VendorListItemDto> ExecuteAsync(
        VendorUpsertRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Vendor name is required.");

        var name = request.Name.Trim();
        var vendorId = request.VendorId == Guid.Empty ? null : request.VendorId;

        var duplicateExists = await _vendorRepository.ExistsByNameAsync(
            name,
            vendorId,
            cancellationToken);

        if (duplicateExists)
            throw new DomainException($"Vendor '{name}' already exists.");

        Vendor vendor;

        if (vendorId.HasValue)
        {
            vendor = await _vendorRepository.GetByIdAsync(vendorId.Value, cancellationToken)
                ?? throw new DomainException($"Vendor '{vendorId.Value}' was not found.");

            vendor.UpdateProfile(name, request.TaxNumber, request.Address, request.Email);
            await _vendorRepository.UpdateAsync(vendor, cancellationToken);

            _logger.LogInformation("Updated vendor {VendorId}", vendor.Id);
        }
        else
        {
            vendor = new Vendor(name, request.TaxNumber, request.Address, request.Email);
            await _vendorRepository.AddAsync(vendor, cancellationToken);

            _logger.LogInformation("Created vendor {VendorId}", vendor.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
