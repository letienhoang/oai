using System.Text.Json;
using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.BackgroundJobs.Uploads;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Uploads.FileDetection;
using OAI.Application.Uploads.Pdf;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Domain.Exceptions;

namespace OAI.Application.BackgroundJobs.Uploads;

public sealed class ProcessBatchFileJob : IProcessBatchFileJob
{
    private readonly IUploadBatchFileRepository _uploadBatchFileRepository;
    private readonly IFileTypeDetectionService _fileTypeDetectionService;
    private readonly IPdfTextExtractionService _pdfTextExtractionService;
    private readonly IPdfPageRenderingService _pdfPageRenderingService;
    private readonly IInvoiceExtractionService _invoiceExtractionService;
    private readonly ICreateInvoiceUseCase _createInvoiceUseCase;
    private readonly IVendorRepository _vendorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessBatchFileJob> _logger;

    public ProcessBatchFileJob(
        IUploadBatchFileRepository uploadBatchFileRepository,
        IFileTypeDetectionService fileTypeDetectionService,
        IPdfTextExtractionService pdfTextExtractionService,
        IPdfPageRenderingService pdfPageRenderingService,
        IInvoiceExtractionService invoiceExtractionService,
        ICreateInvoiceUseCase createInvoiceUseCase,
        IVendorRepository vendorRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessBatchFileJob> logger)
    {
        _uploadBatchFileRepository = uploadBatchFileRepository;
        _fileTypeDetectionService = fileTypeDetectionService;
        _pdfTextExtractionService = pdfTextExtractionService;
        _pdfPageRenderingService = pdfPageRenderingService;
        _invoiceExtractionService = invoiceExtractionService;
        _createInvoiceUseCase = createInvoiceUseCase;
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessAsync(
        Guid uploadBatchFileId,
        CancellationToken cancellationToken = default)
    {
        if (uploadBatchFileId == Guid.Empty)
            throw new ArgumentException("UploadBatchFileId cannot be empty.", nameof(uploadBatchFileId));

        var uploadBatchFile = await _uploadBatchFileRepository.GetByIdAsync(
            uploadBatchFileId,
            cancellationToken);

        if (uploadBatchFile is null)
        {
            _logger.LogWarning(
                "Upload batch file was not found. UploadBatchFileId: {UploadBatchFileId}",
                uploadBatchFileId);

            return;
        }

        if (uploadBatchFile.Status is UploadBatchFileStatus.Processed
            or UploadBatchFileStatus.Failed
            or UploadBatchFileStatus.Skipped
            or UploadBatchFileStatus.Unsupported)
        {
            _logger.LogInformation(
                "Upload batch file is already in a terminal status. UploadBatchFileId: {UploadBatchFileId}, Status: {Status}",
                uploadBatchFile.Id,
                uploadBatchFile.Status);

            return;
        }

        try
        {
            uploadBatchFile.MarkProcessing();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await using var stream = File.OpenRead(uploadBatchFile.StoredFilePath);

            var detection = await _fileTypeDetectionService.DetectAsync(
                stream,
                uploadBatchFile.OriginalFileName,
                uploadBatchFile.ContentType,
                cancellationToken);

            if (detection.FileType == DetectedUploadFileType.Unsupported)
            {
                uploadBatchFile.MarkFailed(
                    "Unsupported file type. Only JPG, PNG, TIFF, PDF and ZIP files are supported.");

                uploadBatchFile.UploadBatch?.RefreshFileCounters();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Upload batch file failed because the file type is unsupported. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, ContentType: {ContentType}, DetectionReason: {DetectionReason}",
                    uploadBatchFile.Id,
                    uploadBatchFile.OriginalFileName,
                    uploadBatchFile.ContentType,
                    detection.Reason);

                return;
            }

            if (detection.FileType == DetectedUploadFileType.Pdf)
            {
                await ProcessPdfAsync(uploadBatchFile, stream, cancellationToken);
                return;
            }

            if (detection.FileType == DetectedUploadFileType.Zip)
            {
                uploadBatchFile.MarkFailed(
                    "ZIP file should be processed by upload package extraction before batch file processing.");

                uploadBatchFile.UploadBatch?.RefreshFileCounters();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Upload batch file failed because ZIP files are not processed directly. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, DetectionReason: {DetectionReason}",
                    uploadBatchFile.Id,
                    uploadBatchFile.OriginalFileName,
                    detection.Reason);

                return;
            }

            _logger.LogInformation(
                "Starting invoice extraction for upload batch file. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, StoredFilePath: {StoredFilePath}",
                uploadBatchFile.Id,
                uploadBatchFile.OriginalFileName,
                uploadBatchFile.StoredFilePath);

            var extracted = await _invoiceExtractionService.ExtractFromFileAsync(
                uploadBatchFile.StoredFilePath,
                cancellationToken);

            if (extracted is null)
            {
                uploadBatchFile.MarkFailed("Invoice extraction failed.");
                uploadBatchFile.UploadBatch?.RefreshFileCounters();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Invoice extraction failed. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}",
                    uploadBatchFile.Id,
                    uploadBatchFile.OriginalFileName);

                return;
            }

            var createdInvoice = await CreateInvoiceFromExtractedAsync(
                uploadBatchFile,
                extracted,
                cancellationToken);

            _logger.LogInformation(
                "Upload batch file processed successfully. UploadBatchFileId: {UploadBatchFileId}, InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
                uploadBatchFile.Id,
                createdInvoice.InvoiceId,
                createdInvoice.InvoiceNumber);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DomainException ex)
        {
            uploadBatchFile.MarkFailed(ex.Message);
            uploadBatchFile.UploadBatch?.RefreshFileCounters();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                ex,
                "Upload batch file processing failed because of domain validation. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}",
                uploadBatchFile.Id,
                uploadBatchFile.OriginalFileName);
        }
        catch (Exception ex)
        {
            uploadBatchFile.MarkRetryPending(
                "Unexpected error occurred while processing upload batch file. The job will be retried by Hangfire.");
            uploadBatchFile.UploadBatch?.RefreshFileCounters();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Upload batch file processing failed unexpectedly. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}",
                uploadBatchFile.Id,
                uploadBatchFile.OriginalFileName);

            throw;
        }
    }

    private async Task ProcessPdfAsync(
        UploadBatchFile uploadBatchFile,
        Stream pdfStream,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting embedded PDF text extraction for upload batch file. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, StoredFilePath: {StoredFilePath}",
            uploadBatchFile.Id,
            uploadBatchFile.OriginalFileName,
            uploadBatchFile.StoredFilePath);

        var extraction = await _pdfTextExtractionService.ExtractAsync(
            pdfStream,
            uploadBatchFile.OriginalFileName,
            cancellationToken);

        if (!extraction.Succeeded)
        {
            uploadBatchFile.MarkFailed(extraction.ErrorMessage ?? "PDF text extraction failed.");
            uploadBatchFile.UploadBatch?.RefreshFileCounters();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Upload batch file failed because embedded PDF text extraction failed. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, ErrorMessage: {ErrorMessage}",
                uploadBatchFile.Id,
                uploadBatchFile.OriginalFileName,
                extraction.ErrorMessage);

            return;
        }

        if (!extraction.HasUsableText)
        {
            var rendering = await RenderScannedPdfPagesAsync(
                uploadBatchFile,
                pdfStream,
                cancellationToken);

            if (!rendering.Succeeded)
            {
                uploadBatchFile.MarkFailed(rendering.ErrorMessage ?? "PDF page rendering failed.");
                uploadBatchFile.UploadBatch?.RefreshFileCounters();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Upload batch file failed because scanned PDF page rendering failed. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, ErrorMessage: {ErrorMessage}",
                    uploadBatchFile.Id,
                    uploadBatchFile.OriginalFileName,
                    rendering.ErrorMessage);

                return;
            }

            uploadBatchFile.MarkFailed(
                $"Scanned PDF pages were rendered successfully: {rendering.Pages.Count} page(s). OCR for rendered PDF pages is not implemented yet.");
            uploadBatchFile.UploadBatch?.RefreshFileCounters();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Upload batch file rendered scanned PDF pages and stopped because rendered-page OCR is not implemented yet. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, PageCount: {PageCount}, RenderedPageCount: {RenderedPageCount}, WarningMessage: {WarningMessage}",
                uploadBatchFile.Id,
                uploadBatchFile.OriginalFileName,
                rendering.PageCount,
                rendering.Pages.Count,
                rendering.WarningMessage ?? extraction.WarningMessage);

            return;
        }

        var extracted = await _invoiceExtractionService.ExtractFromTextAsync(
            extraction.FullText,
            cancellationToken);

        if (extracted is null)
        {
            uploadBatchFile.MarkFailed("Invoice extraction failed from embedded PDF text.");
            uploadBatchFile.UploadBatch?.RefreshFileCounters();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Cannot parse invoice data from embedded PDF text. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, PageCount: {PageCount}",
                uploadBatchFile.Id,
                uploadBatchFile.OriginalFileName,
                extraction.PageCount);

            return;
        }

        var createdInvoice = await CreateInvoiceFromExtractedAsync(
            uploadBatchFile,
            extracted,
            cancellationToken);

        _logger.LogInformation(
            "PDF upload batch file processed successfully from embedded text. UploadBatchFileId: {UploadBatchFileId}, InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}, PageCount: {PageCount}",
            uploadBatchFile.Id,
            createdInvoice.InvoiceId,
            createdInvoice.InvoiceNumber,
            extraction.PageCount);
    }

    private Task<PdfPageRenderingResult> RenderScannedPdfPagesAsync(
        UploadBatchFile uploadBatchFile,
        Stream pdfStream,
        CancellationToken cancellationToken)
    {
        var storedFileDirectory = Path.GetDirectoryName(uploadBatchFile.StoredFilePath);
        if (string.IsNullOrWhiteSpace(storedFileDirectory))
        {
            storedFileDirectory = Directory.GetCurrentDirectory();
        }

        var outputDirectory = Path.Combine(
            storedFileDirectory,
            "rendered",
            uploadBatchFile.Id.ToString("N"));

        var options = new PdfPageRenderingOptions
        {
            OutputDirectory = outputDirectory,
            FileNamePrefix = uploadBatchFile.Id.ToString("N"),
            Dpi = 200,
            MaxPages = 20,
            OverwriteExistingFiles = true
        };

        return _pdfPageRenderingService.RenderAsync(
            pdfStream,
            options,
            cancellationToken);
    }

    private async Task<InvoiceDetailDto> CreateInvoiceFromExtractedAsync(
        UploadBatchFile uploadBatchFile,
        ExtractedInvoiceDto extracted,
        CancellationToken cancellationToken)
    {
        var vendor = await GetOrCreateVendorAsync(extracted, cancellationToken);

        var structuredJson = JsonSerializer.Serialize(
            extracted,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var createRequest = new InvoiceCreateRequestDto
        {
            VendorId = vendor.Id,
            InvoiceNumber = extracted.InvoiceNumber,
            IssueDate = extracted.IssueDate,
            DueDate = extracted.DueDate,
            Currency = extracted.Currency,
            DeclaredSubtotal = extracted.DeclaredSubtotal,
            DeclaredTaxAmount = extracted.DeclaredTaxAmount,
            DeclaredTotalAmount = extracted.DeclaredTotalAmount,
            SourceFileName = uploadBatchFile.OriginalFileName,
            SourceFilePath = uploadBatchFile.StoredFilePath,
            SourceFileContentType = uploadBatchFile.ContentType,
            SourceFileSizeBytes = uploadBatchFile.FileSizeBytes,
            UploadBatchFileId = uploadBatchFile.Id,
            LineItems = extracted.LineItems,
            ExtractionEngineName = extracted.EngineName,
            ExtractionConfidenceScore = extracted.ConfidenceScore,
            ExtractionRawText = extracted.RawText,
            ExtractionStructuredJson = structuredJson
        };

        var createdInvoice = await _createInvoiceUseCase.ExecuteAsync(
            createRequest,
            cancellationToken);

        uploadBatchFile.MarkProcessed(createdInvoice.InvoiceId);
        uploadBatchFile.UploadBatch?.RefreshFileCounters();

        await _uploadBatchFileRepository.UpdateAsync(uploadBatchFile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return createdInvoice;
    }

    private async Task<Vendor> GetOrCreateVendorAsync(
        ExtractedInvoiceDto extracted,
        CancellationToken cancellationToken)
    {
        var vendorName = extracted.VendorName.Trim();

        var vendor = await _vendorRepository.GetByNameAsync(vendorName, cancellationToken);
        if (vendor is not null)
        {
            return vendor;
        }

        vendor = new Vendor(
            name: vendorName,
            taxNumber: extracted.VendorTaxNumber,
            address: extracted.VendorAddress,
            email: extracted.VendorEmail);

        await _vendorRepository.AddAsync(vendor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return vendor;
    }
}
