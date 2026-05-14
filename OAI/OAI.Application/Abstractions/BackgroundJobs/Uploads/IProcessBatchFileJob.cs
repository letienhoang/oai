namespace OAI.Application.Abstractions.BackgroundJobs.Uploads;

public interface IProcessBatchFileJob
{
    Task ProcessAsync(
        Guid uploadBatchFileId,
        CancellationToken cancellationToken = default);
}