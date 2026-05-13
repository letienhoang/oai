using OAI.Domain.Common;

namespace OAI.Domain.Entities;

public sealed class InvoiceSourceFile : Entity
{
    public Guid InvoiceId { get; private set; }
    public Invoice? Invoice { get; private set; }

    public Guid? UploadBatchFileId { get; private set; }

    public string OriginalFileName { get; private set; }
    public string StoredFilePath { get; private set; }
    public string? PreviewFilePath { get; private set; }
    public string ContentType { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int? PageNumber { get; private set; }

    private InvoiceSourceFile()
    {
        OriginalFileName = string.Empty;
        StoredFilePath = string.Empty;
        ContentType = string.Empty;
    }

    public InvoiceSourceFile(
        Guid invoiceId,
        string originalFileName,
        string storedFilePath,
        string contentType,
        long fileSizeBytes,
        Guid? uploadBatchFileId = null,
        string? previewFilePath = null,
        int? pageNumber = null)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty.", nameof(invoiceId));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(storedFilePath))
            throw new ArgumentException("Stored file path is required.", nameof(storedFilePath));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));

        if (fileSizeBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size cannot be negative.");

        if (pageNumber is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

        InvoiceId = invoiceId;
        UploadBatchFileId = uploadBatchFileId;
        OriginalFileName = originalFileName.Trim();
        StoredFilePath = storedFilePath.Trim();
        PreviewFilePath = string.IsNullOrWhiteSpace(previewFilePath) ? null : previewFilePath.Trim();
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        PageNumber = pageNumber;
    }

    public void LinkUploadBatchFile(Guid uploadBatchFileId)
    {
        if (uploadBatchFileId == Guid.Empty)
            throw new ArgumentException("UploadBatchFileId cannot be empty.", nameof(uploadBatchFileId));

        UploadBatchFileId = uploadBatchFileId;
        Touch();
    }

    public void UpdatePreview(string? previewFilePath, int? pageNumber = null)
    {
        if (pageNumber is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

        PreviewFilePath = string.IsNullOrWhiteSpace(previewFilePath) ? null : previewFilePath.Trim();
        PageNumber = pageNumber;
        Touch();
    }
}