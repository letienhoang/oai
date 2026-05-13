using OAI.Domain.Entities;

namespace OAI.Application.Abstractions.Persistence;

public interface IUploadBatchRepository
{
    Task<UploadBatch?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UploadBatch uploadBatch,
        CancellationToken cancellationToken = default);
}