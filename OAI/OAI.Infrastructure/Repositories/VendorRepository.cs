using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
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
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalized = name.Trim().ToUpper();

        return await _context.Vendors
            .AnyAsync(x => x.Name.ToUpper() == normalized, cancellationToken);
    }
}