using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
using OAI.Domain.Entities;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Repositories;

public sealed class UploadBatchRepository : IUploadBatchRepository
{
    private readonly OaiDbContext _dbContext;

    public UploadBatchRepository(OaiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UploadBatch?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadBatches
            .Include(x => x.Files)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        UploadBatch uploadBatch,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.UploadBatches.AddAsync(uploadBatch, cancellationToken);
    }
}