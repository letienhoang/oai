using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.BackgroundJobs.Uploads;
using OAI.Application.Files;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Uploads.FileDetection;
using OAI.Application.Uploads.Pdf;
using OAI.Application.Vendors.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Enums;

namespace OAI.Application.Tests.BackgroundJobs.Uploads;

public sealed class ProcessBatchFileJobTests
{
    [Fact]
    public async Task ProcessAsync_OpensStoredFileThroughFileStorageService()
    {
        var uploadBatchFile = CreateUploadBatchFile("storage/invoices/2026/05/missing-on-disk.png");
        var fileStorage = new FakeFileStorageService(new MemoryStream([0x89, 0x50, 0x4E, 0x47]));
        var detection = new FakeFileTypeDetectionService(
            new FileTypeDetectionResult(
                DetectedUploadFileType.Unsupported,
                null,
                null,
                "test"));

        var job = CreateJob(uploadBatchFile, fileStorage, detection);

        await job.ProcessAsync(uploadBatchFile.Id);

        Assert.Equal(uploadBatchFile.StoredFilePath, fileStorage.OpenedPath);
        Assert.True(detection.WasCalled);
        Assert.Equal(UploadBatchFileStatus.Failed, uploadBatchFile.Status);
    }

    [Fact]
    public async Task ProcessAsync_MarksFileFailedWhenStoredFileIsMissing()
    {
        var uploadBatchFile = CreateUploadBatchFile("storage/invoices/2026/05/missing.png");
        var fileStorage = new FakeFileStorageService(openReadResult: null);
        var unitOfWork = new FakeUnitOfWork();

        var job = CreateJob(
            uploadBatchFile,
            fileStorage,
            new FakeFileTypeDetectionService(),
            unitOfWork);

        await job.ProcessAsync(uploadBatchFile.Id);

        Assert.Equal(UploadBatchFileStatus.Failed, uploadBatchFile.Status);
        Assert.Equal(
            "Uploaded source file was not found in storage. The file may have been moved or the storage path is misconfigured.",
            uploadBatchFile.ErrorMessage);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
    }

    private static ProcessBatchFileJob CreateJob(
        UploadBatchFile uploadBatchFile,
        IFileStorageService fileStorageService,
        IFileTypeDetectionService fileTypeDetectionService,
        IUnitOfWork? unitOfWork = null)
        => new(
            new FakeUploadBatchFileRepository(uploadBatchFile),
            fileStorageService,
            fileTypeDetectionService,
            new ThrowingPdfTextExtractionService(),
            new ThrowingPdfPageRenderingService(),
            new ThrowingPdfPagePreviewStorageService(),
            new ThrowingPdfPageOcrService(),
            new ThrowingInvoiceSourceFileService(),
            new ThrowingInvoiceExtractionService(),
            new ThrowingCreateInvoiceUseCase(),
            new ThrowingVendorRepository(),
            unitOfWork ?? new FakeUnitOfWork(),
            NullLogger<ProcessBatchFileJob>.Instance);

    private static UploadBatchFile CreateUploadBatchFile(string storedFilePath)
    {
        var uploadBatch = new UploadBatch("BATCH-TEST");
        var uploadBatchFile = new UploadBatchFile(
            uploadBatch.Id,
            "invoice.png",
            storedFilePath,
            "image/png",
            4);

        uploadBatch.AddFile(uploadBatchFile);
        uploadBatch.MarkQueued();
        uploadBatchFile.MarkQueued();

        return uploadBatchFile;
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        private readonly Stream? _openReadResult;

        public FakeFileStorageService(Stream? openReadResult)
        {
            _openReadResult = openReadResult;
        }

        public string? OpenedPath { get; private set; }

        public Task<string> SaveAsync(
            string fileName,
            Stream content,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Stream?> OpenReadAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            OpenedPath = path;
            return Task.FromResult(_openReadResult);
        }

        public string GetPhysicalPath(string path)
            => Path.GetFullPath(path);

        public string GetStorageRootPhysicalPath()
            => Directory.GetCurrentDirectory();

        public Task DeleteAsync(
            string path,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeFileTypeDetectionService : IFileTypeDetectionService
    {
        private readonly FileTypeDetectionResult _result;

        public FakeFileTypeDetectionService()
            : this(new FileTypeDetectionResult(DetectedUploadFileType.Unsupported, null, null, "test"))
        {
        }

        public FakeFileTypeDetectionService(FileTypeDetectionResult result)
        {
            _result = result;
        }

        public bool WasCalled { get; private set; }

        public Task<FileTypeDetectionResult> DetectAsync(
            Stream fileStream,
            string? fileName,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeUploadBatchFileRepository : IUploadBatchFileRepository
    {
        private readonly UploadBatchFile _uploadBatchFile;

        public FakeUploadBatchFileRepository(UploadBatchFile uploadBatchFile)
        {
            _uploadBatchFile = uploadBatchFile;
        }

        public Task<UploadBatchFile?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult<UploadBatchFile?>(id == _uploadBatchFile.Id ? _uploadBatchFile : null);

        public Task UpdateAsync(
            UploadBatchFile uploadBatchFile,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class ThrowingPdfTextExtractionService : IPdfTextExtractionService
    {
        public Task<PdfTextExtractionResult> ExtractAsync(
            Stream pdfStream,
            string? fileName,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingPdfPageRenderingService : IPdfPageRenderingService
    {
        public Task<PdfPageRenderingResult> RenderAsync(
            Stream pdfStream,
            PdfPageRenderingOptions options,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingPdfPagePreviewStorageService : IPdfPagePreviewStorageService
    {
        public Task<PdfPagePreviewStorageResult> StoreAsync(
            Guid uploadBatchFileId,
            IReadOnlyList<PdfRenderedPageResult> renderedPages,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task LinkPreviewsToInvoiceAsync(
            Guid uploadBatchFileId,
            Guid invoiceId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingPdfPageOcrService : IPdfPageOcrService
    {
        public Task<PdfPageOcrResult> OcrAsync(
            IReadOnlyList<PdfStoredPagePreviewResult> previews,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingInvoiceSourceFileService : IInvoiceSourceFileService
    {
        public Task AddOriginalSourceFileAsync(
            Guid invoiceId,
            Guid uploadBatchFileId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task LinkSourceFilesToInvoiceAsync(
            Guid invoiceId,
            Guid uploadBatchFileId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingInvoiceExtractionService : IInvoiceExtractionService
    {
        public Task<ExtractedInvoiceDto?> ExtractFromFileAsync(
            string filePath,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ExtractedInvoiceDto?> ExtractFromTextAsync(
            string rawText,
            string sourceName = "raw-text",
            decimal confidenceScore = 1.0m,
            string engineName = "RawText",
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingCreateInvoiceUseCase : ICreateInvoiceUseCase
    {
        public Task<InvoiceDetailDto> ExecuteAsync(
            InvoiceCreateRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingVendorRepository : IVendorRepository
    {
        public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Vendor?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Vendor>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            VendorFilterDto filter,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<int> CountAsync(
            VendorFilterDto filter,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludedVendorId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
