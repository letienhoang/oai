using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Application.Uploads.Dtos;
using OAI.Domain.Entities;

namespace OAI.Application.Services;

public sealed class UploadPackageService : IUploadPackageService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff",
        ".pdf",
        ".zip"
    };

    private readonly IFileStorageService _fileStorageService;
    private readonly IUploadBatchRepository _uploadBatchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadPackageService(
        IFileStorageService fileStorageService,
        IUploadBatchRepository uploadBatchRepository,
        IUnitOfWork unitOfWork)
    {
        _fileStorageService = fileStorageService;
        _uploadBatchRepository = uploadBatchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateUploadPackageResultDto> CreateAsync(
        CreateUploadPackageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("File name is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ContentType))
            throw new ArgumentException("Content type is required.", nameof(request));

        if (request.FileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "File size must be greater than zero.");

        var safeFileName = Path.GetFileName(request.FileName);
        var extension = Path.GetExtension(safeFileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported file format. Allowed formats: jpg, jpeg, png, tif, tiff, pdf, zip.");

        var storedFilePath = await _fileStorageService.SaveAsync(
            safeFileName,
            request.Content,
            cancellationToken);

        var batchCode = GenerateBatchCode();

        var isZip = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);

        var uploadBatch = new UploadBatch(
            batchCode,
            request.UploadedByUserId,
            request.UploadedByUserName,
            originalZipFilePath: isZip ? storedFilePath : null);

        var uploadBatchFile = new UploadBatchFile(
            uploadBatch.Id,
            safeFileName,
            storedFilePath,
            NormalizeContentType(request.ContentType, extension),
            request.FileSizeBytes);

        uploadBatch.AddFile(uploadBatchFile);
        uploadBatch.MarkQueued();
        uploadBatchFile.MarkQueued();

        await _uploadBatchRepository.AddAsync(uploadBatch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateUploadPackageResultDto(
            uploadBatch.Id,
            uploadBatch.BatchCode,
            uploadBatch.TotalFiles,
            uploadBatch.Status,
            new[]
            {
                new UploadBatchFileResultDto(
                    uploadBatchFile.Id,
                    uploadBatchFile.OriginalFileName,
                    uploadBatchFile.StoredFilePath,
                    uploadBatchFile.ContentType,
                    uploadBatchFile.FileSizeBytes,
                    uploadBatchFile.Status)
            });
    }

    private static string GenerateBatchCode()
        => $"BATCH-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..33];

    private static string NormalizeContentType(string contentType, string extension)
    {
        if (!string.IsNullOrWhiteSpace(contentType) &&
            !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType.Trim();
        }

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tif" or ".tiff" => "image/tiff",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}