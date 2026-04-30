using OAI.Application.Abstractions.Persistence;
using OAI.Domain.Entities;

namespace OAI.Application.Tests.Fakes;

public sealed class FakeVendorRepository : IVendorRepository
{
    private readonly List<Vendor> _vendors = new();

    public IReadOnlyList<Vendor> Vendors => _vendors.AsReadOnly();

    public Task<Vendor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_vendors.FirstOrDefault(x => x.Id == id));
    }

    public Task<Vendor?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_vendors.FirstOrDefault(x =>
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IReadOnlyList<Vendor>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Vendor>>(_vendors.AsReadOnly());
    }

    public Task AddAsync(
        Vendor vendor,
        CancellationToken cancellationToken = default)
    {
        _vendors.Add(vendor);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        Vendor vendor,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var exists = _vendors.Any(x =>
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public void Seed(Vendor vendor)
    {
        _vendors.Add(vendor);
    }
}