using OAI.Application.Abstractions.Persistence;
using OAI.Application.Vendors.Dtos;
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

    public Task<IReadOnlyList<Vendor>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        VendorFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(filter);

        query = filter.SortBy == VendorSortFields.CreatedAt
            ? filter.SortDescending
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
            : filter.SortDescending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name);

        var vendors = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<Vendor>>(vendors);
    }

    public Task<int> CountAsync(
        VendorFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApplyFilter(filter).Count());
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
        return ExistsByNameAsync(name, excludedVendorId: null, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedVendorId,
        CancellationToken cancellationToken = default)
    {
        var exists = _vendors.Any(x =>
            x.Id != excludedVendorId &&
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    public void Seed(Vendor vendor)
    {
        _vendors.Add(vendor);
    }

    private IEnumerable<Vendor> ApplyFilter(VendorFilterDto filter)
    {
        IEnumerable<Vendor> query = _vendors;

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();

            query = query.Where(x =>
                x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (x.TaxNumber?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Email?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Address?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query;
    }
}
