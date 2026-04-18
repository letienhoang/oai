using OAI.Domain.Entities;

namespace OAI.Application.Abstractions.Persistence;

public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Vendor?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}