using OAI.Domain.Common;
using OAI.Domain.Enums;

namespace OAI.Domain.Entities;

public sealed class UploadBatchFile : Entity
{
    public Guid UploadBatchId { get; private set; }
    public UploadBatch? UploadBatch { get; private set; }

    public string OriginalFileName { get; private set; }
    public string StoredFilePath { get; private set; }
    public string ContentType { get; private set; }
    public long FileSizeBytes { get; private set; }

    public UploadBatchFileStatus Status { get; private set; }

    public Guid? InvoiceId { get; private set; }
    public Invoice? Invoice { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public DateTimeOffset? ProcessingCompletedAt { get; private set; }

    private UploadBatchFile()
    {
        OriginalFileName = string.Empty;
        StoredFilePath = string.Empty;
        ContentType = string.Empty;
        Status = UploadBatchFileStatus.Created;
    }

    public UploadBatchFile(
        Guid uploadBatchId,
        string originalFileName,
        string storedFilePath,
        string contentType,
        long fileSizeBytes)
    {
        if (uploadBatchId == Guid.Empty)
            throw new ArgumentException("UploadBatchId cannot be empty.", nameof(uploadBatchId));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(storedFilePath))
            throw new ArgumentException("Stored file path is required.", nameof(storedFilePath));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        if (fileSizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size cannot be negative.");

        UploadBatchId = uploadBatchId;
        OriginalFileName = originalFileName.Trim();
        StoredFilePath = storedFilePath.Trim();
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        Status = UploadBatchFileStatus.Created;
    }

    public void MarkQueued()
    {
        Status = UploadBatchFileStatus.Queued;
        ErrorMessage = null;
        Touch();
    }

    public void MarkProcessing()
    {
        ProcessingStartedAt ??= DateTimeOffset.UtcNow;
        Status = UploadBatchFileStatus.Processing;
        ErrorMessage = null;
        Touch();
    }

    public void MarkProcessed(Guid invoiceId)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty.", nameof(invoiceId));

        InvoiceId = invoiceId;
        Status = UploadBatchFileStatus.Processed;
        ErrorMessage = null;
        ProcessingCompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message is required.", nameof(errorMessage));

        Status = UploadBatchFileStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        ProcessingCompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkSkipped(string? reason = null)
    {
        Status = UploadBatchFileStatus.Skipped;
        ErrorMessage = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ProcessingCompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkUnsupported(string? reason = null)
    {
        Status = UploadBatchFileStatus.Unsupported;
        ErrorMessage = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ProcessingCompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }
    
    public void MarkRetryPending(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message is required.", nameof(errorMessage));

        Status = UploadBatchFileStatus.RetryPending;
        ErrorMessage = errorMessage.Trim();
        ProcessingCompletedAt = null;
        Touch();
    }
}