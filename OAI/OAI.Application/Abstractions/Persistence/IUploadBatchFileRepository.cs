using OAI.Domain.Entities;

namespace OAI.Application.Abstractions.Persistence;

public interface IUploadBatchFileRepository
{
    Task<UploadBatchFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UploadBatchFile uploadBatchFile,
        CancellationToken cancellationToken = default);
}