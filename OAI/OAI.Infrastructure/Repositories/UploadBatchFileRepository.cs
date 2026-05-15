using Microsoft.EntityFrameworkCore;
using OAI.Application.Abstractions.Persistence;
using OAI.Domain.Entities;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Repositories;

public sealed class UploadBatchFileRepository : IUploadBatchFileRepository
{
    private readonly OaiDbContext _dbContext;

    public UploadBatchFileRepository(OaiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UploadBatchFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadBatchFiles
            .Include(x => x.UploadBatch)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task UpdateAsync(
        UploadBatchFile uploadBatchFile,
        CancellationToken cancellationToken = default)
    {
        _dbContext.UploadBatchFiles.Update(uploadBatchFile);
        return Task.CompletedTask;
    }
}