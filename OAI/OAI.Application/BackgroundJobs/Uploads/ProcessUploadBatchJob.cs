using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.BackgroundJobs.Uploads;

namespace OAI.Application.BackgroundJobs.Uploads;

public sealed class ProcessUploadBatchJob : IProcessUploadBatchJob
{
    private readonly ILogger<ProcessUploadBatchJob> _logger;

    public ProcessUploadBatchJob(
        ILogger<ProcessUploadBatchJob> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(
        Guid uploadBatchId,
        CancellationToken cancellationToken = default)
    {
        if (uploadBatchId == Guid.Empty)
            throw new ArgumentException("UploadBatchId cannot be empty.", nameof(uploadBatchId));

        _logger.LogInformation(
            "Process upload batch job was enqueued. UploadBatchId: {UploadBatchId}. Actual processing will be implemented in T111.",
            uploadBatchId);

        return Task.CompletedTask;
    }
}