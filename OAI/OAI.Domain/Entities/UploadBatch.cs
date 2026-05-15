using OAI.Domain.Common;
using OAI.Domain.Enums;

namespace OAI.Domain.Entities;

public sealed class UploadBatch : Entity
{
    private readonly List<UploadBatchFile> _files = new();
    
    public string BatchCode { get; private set; }

    public Guid? UploadedByUserId { get; private set; }
    public string? UploadedByUserName { get; private set; }

    public int TotalFiles { get; private set; }
    public int ProcessedFiles { get; private set; }
    public int FailedFiles { get; private set; }

    public string? OriginalZipFilePath { get; private set; }

    public UploadBatchStatus Status { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    
    public IReadOnlyCollection<UploadBatchFile> Files => _files.AsReadOnly();

    private UploadBatch()
    {
        BatchCode = string.Empty;
        Status = UploadBatchStatus.Created;
    }

    public UploadBatch(
        string batchCode,
        Guid? uploadedByUserId = null,
        string? uploadedByUserName = null,
        string? originalZipFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(batchCode))
            throw new ArgumentException("Batch code is required.", nameof(batchCode));

        BatchCode = batchCode.Trim();
        UploadedByUserId = uploadedByUserId;
        UploadedByUserName = string.IsNullOrWhiteSpace(uploadedByUserName)
            ? null
            : uploadedByUserName.Trim();
        OriginalZipFilePath = string.IsNullOrWhiteSpace(originalZipFilePath)
            ? null
            : originalZipFilePath.Trim();

        Status = UploadBatchStatus.Created;
    }

    public void SetTotalFiles(int totalFiles)
    {
        if (totalFiles < 0)
            throw new ArgumentOutOfRangeException(nameof(totalFiles), "Total files cannot be negative.");

        TotalFiles = totalFiles;
        Touch();
    }

    public void MarkQueued()
    {
        Status = UploadBatchStatus.Queued;
        Touch();
    }

    public void MarkProcessing()
    {
        if (StartedAt is null)
            StartedAt = DateTimeOffset.UtcNow;

        Status = UploadBatchStatus.Processing;
        Touch();
    }

    public void IncrementProcessedFiles()
    {
        ProcessedFiles++;

        if (ProcessedFiles > TotalFiles && TotalFiles > 0)
            ProcessedFiles = TotalFiles;

        RefreshCompletionStatus();
        Touch();
    }

    public void IncrementFailedFiles()
    {
        FailedFiles++;

        if (FailedFiles > TotalFiles && TotalFiles > 0)
            FailedFiles = TotalFiles;

        RefreshCompletionStatus();
        Touch();
    }

    public void MarkCompleted()
    {
        Status = FailedFiles > 0
            ? UploadBatchStatus.PartiallyFailed
            : UploadBatchStatus.Completed;

        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkFailed()
    {
        Status = UploadBatchStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkCancelled()
    {
        Status = UploadBatchStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }
    
    public void AddFile(UploadBatchFile file)
    {
        if (file is null)
            throw new ArgumentNullException(nameof(file));

        if (file.UploadBatchId != Id)
            throw new ArgumentException("Upload batch file does not belong to this batch.", nameof(file));

        _files.Add(file);
        TotalFiles = _files.Count;
        Touch();
    }
    
    public void RefreshFileCounters()
    {
        ProcessedFiles = _files.Count(x => x.Status == UploadBatchFileStatus.Processed);

        FailedFiles = _files.Count(x =>
            x.Status == UploadBatchFileStatus.Failed ||
            x.Status == UploadBatchFileStatus.Unsupported ||
            x.Status == UploadBatchFileStatus.Skipped);

        RefreshCompletionStatus();
        Touch();
    }

    private void RefreshCompletionStatus()
    {
        if (TotalFiles <= 0)
            return;

        var finishedFiles = ProcessedFiles + FailedFiles;

        if (finishedFiles < TotalFiles)
            return;

        Status = FailedFiles > 0
            ? UploadBatchStatus.PartiallyFailed
            : UploadBatchStatus.Completed;

        CompletedAt ??= DateTimeOffset.UtcNow;
    }
}