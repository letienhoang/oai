using System.Text.Json;
using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Entities;
using OAI.Domain.Exceptions;

namespace OAI.Application.Services;

public sealed class InvoiceProcessingService : IInvoiceProcessingService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IInvoiceExtractionService _invoiceExtractionService;
    private readonly ICreateInvoiceUseCase _createInvoiceUseCase;
    private readonly IVendorRepository _vendorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvoiceProcessingService> _logger;

    public InvoiceProcessingService(
        IFileStorageService fileStorageService,
        IInvoiceExtractionService invoiceExtractionService,
        ICreateInvoiceUseCase createInvoiceUseCase,
        IVendorRepository vendorRepository,
        IUnitOfWork unitOfWork,
        ILogger<InvoiceProcessingService> logger)
    {
        _fileStorageService = fileStorageService;
        _invoiceExtractionService = invoiceExtractionService;
        _createInvoiceUseCase = createInvoiceUseCase;
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InvoiceUploadResultDto> UploadInvoiceAsync(
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["FileName"] = fileName
        });

        _logger.LogInformation("Start uploading invoice file {FileName}", fileName);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            _logger.LogWarning("Upload invoice failed because file name is empty");
            throw new DomainException("File name is required.");
        }

        if (fileStream is null || !fileStream.CanRead)
        {
            _logger.LogWarning("Upload invoice failed because file stream is invalid for file {FileName}", fileName);
            throw new DomainException("File stream is invalid.");
        }

        var savedPath = await _fileStorageService.SaveAsync(fileName, fileStream, cancellationToken);

        _logger.LogInformation("Invoice file {FileName} saved to {SavedPath}", fileName, savedPath);

        var extracted = await _invoiceExtractionService.ExtractFromFileAsync(savedPath, cancellationToken);
        if (extracted is null)
        {
            _logger.LogWarning("Invoice extraction failed for file {FileName} at {SavedPath}", fileName, savedPath);

            return new InvoiceUploadResultDto
            {
                InvoiceId = Guid.Empty,
                FileName = fileName,
                Status = "Failed",
                Message = "Invoice extraction failed."
            };
        }

        _logger.LogInformation(
            "Invoice extraction completed for file {FileName}. InvoiceNumber: {InvoiceNumber}, Confidence: {ConfidenceScore}",
            fileName,
            extracted.InvoiceNumber,
            extracted.ConfidenceScore);

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
            SourceFileName = fileName,
            SourceFilePath = savedPath,
            LineItems = extracted.LineItems,
            ExtractionEngineName = extracted.EngineName,
            ExtractionConfidenceScore = extracted.ConfidenceScore,
            ExtractionRawText = extracted.RawText,
            ExtractionStructuredJson = structuredJson
        };

        InvoiceDetailDto createdInvoice;
        try
        {
            createdInvoice = await _createInvoiceUseCase.ExecuteAsync(createRequest, cancellationToken);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex,
                "Invoice processing failed because of domain validation. FileName: {FileName}, InvoiceNumber: {InvoiceNumber}",
                fileName,
                extracted.InvoiceNumber);

            return new InvoiceUploadResultDto
            {
                InvoiceId = Guid.Empty,
                FileName = fileName,
                Status = "Failed",
                Message = ex.Message
            };
        }

        _logger.LogInformation(
            "Invoice created successfully. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
            createdInvoice.InvoiceId,
            createdInvoice.InvoiceNumber);

        return new InvoiceUploadResultDto
        {
            InvoiceId = createdInvoice.InvoiceId,
            FileName = fileName,
            Status = "Processed",
            Message = $"Invoice uploaded and processed successfully. Confidence: {extracted.ConfidenceScore:P0}"
        };
    }

    public async Task<InvoiceDetailDto?> ProcessInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ProcessInvoiceAsync called for InvoiceId {InvoiceId}", invoiceId);

        await Task.CompletedTask;
        return null;
    }

    public async Task<InvoiceDetailDto?> ReprocessInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ReprocessInvoiceAsync called for InvoiceId {InvoiceId}", invoiceId);

        await Task.CompletedTask;
        return null;
    }

    private async Task<Vendor> GetOrCreateVendorAsync(
        ExtractedInvoiceDto extracted,
        CancellationToken cancellationToken)
    {
        var vendorName = extracted.VendorName.Trim();

        var vendor = await _vendorRepository.GetByNameAsync(vendorName, cancellationToken);
        if (vendor is not null)
        {
            _logger.LogInformation("Existing vendor found. VendorId: {VendorId}, VendorName: {VendorName}", vendor.Id, vendor.Name);
            return vendor;
        }

        vendor = new Vendor(
            name: vendorName,
            taxNumber: extracted.VendorTaxNumber,
            address: extracted.VendorAddress,
            email: extracted.VendorEmail);

        await _vendorRepository.AddAsync(vendor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New vendor created. VendorId: {VendorId}, VendorName: {VendorName}", vendor.Id, vendor.Name);

        return vendor;
    }
}