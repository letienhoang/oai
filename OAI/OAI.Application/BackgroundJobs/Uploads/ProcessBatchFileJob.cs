using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.BackgroundJobs.Uploads;

namespace OAI.Application.BackgroundJobs.Uploads;

public sealed class ProcessBatchFileJob : IProcessBatchFileJob
{
    private readonly ILogger<ProcessBatchFileJob> _logger;

    public ProcessBatchFileJob(
        ILogger<ProcessBatchFileJob> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(
        Guid uploadBatchFileId,
        CancellationToken cancellationToken = default)
    {
        if (uploadBatchFileId == Guid.Empty)
            throw new ArgumentException("UploadBatchFileId cannot be empty.", nameof(uploadBatchFileId));

        _logger.LogInformation(
            "Process batch file job was enqueued. UploadBatchFileId: {UploadBatchFileId}. Actual file processing will be implemented in T112.",
            uploadBatchFileId);

        return Task.CompletedTask;
    }
}