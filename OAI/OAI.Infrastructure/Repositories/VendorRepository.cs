using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Vendors.Dtos;
using OAI.Domain.Entities;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Repositories;

public sealed class VendorRepository : IVendorRepository
{
    private readonly OaiDbContext _context;

    public VendorRepository(OaiDbContext context)
    {
        _context = context;
    }

    public async Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vendors
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Vendor?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = name.Trim().ToUpper();

        return await _context.Vendors
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Vendors
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        VendorFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.Vendors.AsNoTracking(), filter);
        query = ApplySorting(query, filter);

        return await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        VendorFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.Vendors.AsNoTracking(), filter);

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        await _context.Vendors.AddAsync(vendor, cancellationToken);
    }

    public Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        _context.Vendors.Update(vendor);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await ExistsByNameAsync(name, excludedVendorId: null, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedVendorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalized = name.Trim().ToUpper();
        var query = _context.Vendors.AsQueryable();

        if (excludedVendorId.HasValue)
            query = query.Where(x => x.Id != excludedVendorId.Value);

        return await query.AnyAsync(x => x.Name.ToUpper() == normalized, cancellationToken);
    }

    private static IQueryable<Vendor> ApplyFilter(IQueryable<Vendor> query, VendorFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();

            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                (x.TaxNumber != null && x.TaxNumber.Contains(keyword)) ||
                (x.Email != null && x.Email.Contains(keyword)) ||
                (x.Address != null && x.Address.Contains(keyword)));
        }

        return query;
    }

    private static IOrderedQueryable<Vendor> ApplySorting(
        IQueryable<Vendor> query,
        VendorFilterDto filter)
    {
        var sortBy = filter.SortBy?.Trim();

        return sortBy switch
        {
            VendorSortFields.CreatedAt => filter.SortDescending
                ? query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Name)
                : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Name),
            _ => filter.SortDescending
                ? query.OrderByDescending(x => x.Name).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.Name).ThenByDescending(x => x.CreatedAt)
        };
    }
}
