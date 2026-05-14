using System.Text.Json;
using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.BackgroundJobs.Uploads;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Domain.Exceptions;

namespace OAI.Application.BackgroundJobs.Uploads;

public sealed class ProcessBatchFileJob : IProcessBatchFileJob
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff"
    };

    private readonly IUploadBatchFileRepository _uploadBatchFileRepository;
    private readonly IInvoiceExtractionService _invoiceExtractionService;
    private readonly ICreateInvoiceUseCase _createInvoiceUseCase;
    private readonly IVendorRepository _vendorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessBatchFileJob> _logger;

    public ProcessBatchFileJob(
        IUploadBatchFileRepository uploadBatchFileRepository,
        IInvoiceExtractionService invoiceExtractionService,
        ICreateInvoiceUseCase createInvoiceUseCase,
        IVendorRepository vendorRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessBatchFileJob> logger)
    {
        _uploadBatchFileRepository = uploadBatchFileRepository;
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

            var extension = Path.GetExtension(uploadBatchFile.OriginalFileName);

            if (!SupportedImageExtensions.Contains(extension))
            {
                uploadBatchFile.MarkUnsupported(
                    "Only image files are processed in T112. PDF processing will be implemented in Phase 10D.");

                uploadBatchFile.UploadBatch?.RefreshFileCounters();

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Upload batch file marked as unsupported. UploadBatchFileId: {UploadBatchFileId}, FileName: {FileName}, ContentType: {ContentType}",
                    uploadBatchFile.Id,
                    uploadBatchFile.OriginalFileName,
                    uploadBatchFile.ContentType);

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
            uploadBatchFile.MarkFailed("Unexpected error occurred while processing upload batch file.");
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