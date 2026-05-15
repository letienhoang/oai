namespace OAI.Application.Abstractions.BackgroundJobs.Uploads;

public interface IProcessUploadBatchJob
{
    Task ProcessAsync(
        Guid uploadBatchId,
        CancellationToken cancellationToken = default);
}