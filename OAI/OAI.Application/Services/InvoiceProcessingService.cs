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

    public InvoiceProcessingService(
        IFileStorageService fileStorageService,
        IInvoiceExtractionService invoiceExtractionService,
        ICreateInvoiceUseCase createInvoiceUseCase,
        IVendorRepository vendorRepository,
        IUnitOfWork unitOfWork)
    {
        _fileStorageService = fileStorageService;
        _invoiceExtractionService = invoiceExtractionService;
        _createInvoiceUseCase = createInvoiceUseCase;
        _vendorRepository = vendorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvoiceUploadResultDto> UploadInvoiceAsync(
        string fileName,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("File name is required.");

        if (fileStream is null || !fileStream.CanRead)
            throw new DomainException("File stream is invalid.");

        var savedPath = await _fileStorageService.SaveAsync(fileName, fileStream, cancellationToken);

        var extracted = await _invoiceExtractionService.ExtractFromFileAsync(savedPath, cancellationToken);
        if (extracted is null)
        {
            return new InvoiceUploadResultDto
            {
                InvoiceId = Guid.Empty,
                FileName = fileName,
                Status = "Failed",
                Message = "Invoice extraction failed."
            };
        }

        var vendor = await GetOrCreateVendorAsync(extracted, cancellationToken);

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
            LineItems = extracted.LineItems
        };

        var createdInvoice = await _createInvoiceUseCase.ExecuteAsync(createRequest, cancellationToken);

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
        // The initial stage can be left blank or implemented in a later step.
        // When you add the repository, load invoice + source file path,
        // this method will be used to re-run OCR/extraction/validation.
        await Task.CompletedTask;
        return null;
    }

    public async Task<InvoiceDetailDto?> ReprocessInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        // Similar to ProcessInvoiceAsync, this can be implemented later.
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
            return vendor;

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