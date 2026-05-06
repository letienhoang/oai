using OAI.Application.Abstractions.Persistence;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OaiDbContext _context;

    public UnitOfWork(OaiDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}