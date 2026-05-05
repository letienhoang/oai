using OAI.Domain.Entities;

using OAI.Application.Vendors.Dtos;

namespace OAI.Application.Abstractions.Persistence;

public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Vendor?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        VendorFilterDto filter,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(
        VendorFilterDto filter,
        CancellationToken cancellationToken = default);

    Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedVendorId,
        CancellationToken cancellationToken = default);
}
