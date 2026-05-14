using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.BackgroundJobs;
using OAI.Application.Abstractions.BackgroundJobs.Uploads;
using OAI.Application.Abstractions.Persistence;
using OAI.Domain.Enums;

namespace OAI.Application.BackgroundJobs.Uploads;

public sealed class ProcessUploadBatchJob : IProcessUploadBatchJob
{
    private readonly IUploadBatchRepository _uploadBatchRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessUploadBatchJob> _logger;

    public ProcessUploadBatchJob(
        IUploadBatchRepository uploadBatchRepository,
        IBackgroundJobClient backgroundJobClient,
        IUnitOfWork unitOfWork,
        ILogger<ProcessUploadBatchJob> logger)
    {
        _uploadBatchRepository = uploadBatchRepository;
        _backgroundJobClient = backgroundJobClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessAsync(
        Guid uploadBatchId,
        CancellationToken cancellationToken = default)
    {
        if (uploadBatchId == Guid.Empty)
            throw new ArgumentException("UploadBatchId cannot be empty.", nameof(uploadBatchId));

        var uploadBatch = await _uploadBatchRepository.GetByIdAsync(
            uploadBatchId,
            cancellationToken);

        if (uploadBatch is null)
        {
            _logger.LogWarning(
                "Upload batch was not found. UploadBatchId: {UploadBatchId}",
                uploadBatchId);

            return;
        }

        if (uploadBatch.Files.Count == 0)
        {
            _logger.LogWarning(
                "Upload batch has no files. UploadBatchId: {UploadBatchId}, BatchCode: {BatchCode}",
                uploadBatch.Id,
                uploadBatch.BatchCode);

            uploadBatch.MarkFailed();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return;
        }

        if (uploadBatch.Status is UploadBatchStatus.Completed
            or UploadBatchStatus.PartiallyFailed
            or UploadBatchStatus.Failed
            or UploadBatchStatus.Cancelled)
        {
            _logger.LogInformation(
                "Upload batch is already in a terminal status. UploadBatchId: {UploadBatchId}, BatchCode: {BatchCode}, Status: {Status}",
                uploadBatch.Id,
                uploadBatch.BatchCode,
                uploadBatch.Status);

            return;
        }

        uploadBatch.MarkProcessing();

        var enqueuedFileCount = 0;

        foreach (var file in uploadBatch.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Status is UploadBatchFileStatus.Processed
                or UploadBatchFileStatus.Failed
                or UploadBatchFileStatus.Skipped
                or UploadBatchFileStatus.Unsupported)
            {
                _logger.LogInformation(
                    "Skipping upload batch file because it is already in a terminal status. UploadBatchId: {UploadBatchId}, UploadBatchFileId: {UploadBatchFileId}, Status: {Status}",
                    uploadBatch.Id,
                    file.Id,
                    file.Status);

                continue;
            }

            file.MarkQueued();

            var backgroundJobId = await _backgroundJobClient.EnqueueAsync<IProcessBatchFileJob>(
                job => job.ProcessAsync(file.Id, CancellationToken.None),
                BackgroundJobQueues.Uploads,
                cancellationToken);

            enqueuedFileCount++;

            _logger.LogInformation(
                "Process batch file job enqueued. UploadBatchId: {UploadBatchId}, UploadBatchFileId: {UploadBatchFileId}, BackgroundJobId: {BackgroundJobId}",
                uploadBatch.Id,
                file.Id,
                backgroundJobId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Upload batch processing dispatch completed. UploadBatchId: {UploadBatchId}, BatchCode: {BatchCode}, TotalFiles: {TotalFiles}, EnqueuedFileCount: {EnqueuedFileCount}",
            uploadBatch.Id,
            uploadBatch.BatchCode,
            uploadBatch.TotalFiles,
            enqueuedFileCount);
    }
}