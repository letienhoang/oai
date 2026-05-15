using System.IO.Compression;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Application.Uploads.Dtos;
using OAI.Domain.Entities;

namespace OAI.Application.Services;

public sealed class UploadPackageService : IUploadPackageService
{
    private const int MaxZipFileCount = 50;
    private const long MaxZipTotalUncompressedBytes = 100 * 1024 * 1024;
    private const long MaxZipEntryUncompressedBytes = 20 * 1024 * 1024;

    private static readonly HashSet<string> PackageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff",
        ".pdf",
        ".zip"
    };

    private static readonly HashSet<string> ProcessableFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff",
        ".pdf"
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

        if (string.IsNullOrWhiteSpace(extension) || !PackageExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                "Unsupported file format. Allowed formats: jpg, jpeg, png, tif, tiff, pdf, zip.");
        }

        await using var packageContent = await CopyToSeekableStreamAsync(
            request.Content,
            cancellationToken);

        var storedPackagePath = await SavePackageAsync(
            safeFileName,
            packageContent,
            cancellationToken);

        var batchCode = GenerateBatchCode();
        var isZip = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);

        var uploadBatch = new UploadBatch(
            batchCode,
            request.UploadedByUserId,
            request.UploadedByUserName,
            originalZipFilePath: isZip ? storedPackagePath : null);

        if (isZip)
        {
            await AddZipEntriesAsync(
                uploadBatch,
                packageContent,
                cancellationToken);
        }
        else
        {
            AddSingleFile(
                uploadBatch,
                safeFileName,
                storedPackagePath,
                NormalizeContentType(request.ContentType, extension),
                request.FileSizeBytes);
        }

        if (uploadBatch.TotalFiles <= 0)
        {
            throw new InvalidOperationException(
                "The upload package does not contain any supported files.");
        }

        uploadBatch.MarkQueued();

        foreach (var file in uploadBatch.Files)
        {
            file.MarkQueued();
        }

        await _uploadBatchRepository.AddAsync(uploadBatch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateUploadPackageResultDto(
            uploadBatch.Id,
            uploadBatch.BatchCode,
            uploadBatch.TotalFiles,
            uploadBatch.Status,
            uploadBatch.Files
                .Select(file => new UploadBatchFileResultDto(
                    file.Id,
                    file.OriginalFileName,
                    file.StoredFilePath,
                    file.ContentType,
                    file.FileSizeBytes,
                    file.Status))
                .ToArray());
    }

    private static async Task<MemoryStream> CopyToSeekableStreamAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();

        await source.CopyToAsync(memoryStream, cancellationToken);

        memoryStream.Position = 0;

        return memoryStream;
    }

    private async Task<string> SavePackageAsync(
        string safeFileName,
        Stream packageContent,
        CancellationToken cancellationToken)
    {
        packageContent.Position = 0;

        var storedPackagePath = await _fileStorageService.SaveAsync(
            safeFileName,
            packageContent,
            cancellationToken);

        packageContent.Position = 0;

        return storedPackagePath;
    }

    private void AddSingleFile(
        UploadBatch uploadBatch,
        string originalFileName,
        string storedFilePath,
        string contentType,
        long fileSizeBytes)
    {
        var uploadBatchFile = new UploadBatchFile(
            uploadBatch.Id,
            originalFileName,
            storedFilePath,
            contentType,
            fileSizeBytes);

        uploadBatch.AddFile(uploadBatchFile);
    }

    private async Task AddZipEntriesAsync(
        UploadBatch uploadBatch,
        Stream zipContent,
        CancellationToken cancellationToken)
    {
        zipContent.Position = 0;

        using var archive = new ZipArchive(
            zipContent,
            ZipArchiveMode.Read,
            leaveOpen: true);

        var acceptedFileCount = 0;
        long totalUncompressedBytes = 0;

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsDirectory(entry))
                continue;

            if (!IsSafeZipEntryName(entry.FullName))
                continue;

            var entryFileName = Path.GetFileName(entry.FullName);

            if (string.IsNullOrWhiteSpace(entryFileName))
                continue;

            var entryExtension = Path.GetExtension(entryFileName);

            if (string.IsNullOrWhiteSpace(entryExtension) ||
                !ProcessableFileExtensions.Contains(entryExtension))
            {
                continue;
            }

            if (entry.Length <= 0)
                continue;

            if (entry.Length > MaxZipEntryUncompressedBytes)
            {
                throw new InvalidOperationException(
                    $"ZIP entry '{entry.FullName}' exceeds the {MaxZipEntryUncompressedBytes} bytes per-file limit.");
            }

            totalUncompressedBytes += entry.Length;

            if (totalUncompressedBytes > MaxZipTotalUncompressedBytes)
            {
                throw new InvalidOperationException(
                    $"ZIP package exceeds the {MaxZipTotalUncompressedBytes} bytes total uncompressed limit.");
            }

            acceptedFileCount++;

            if (acceptedFileCount > MaxZipFileCount)
            {
                throw new InvalidOperationException(
                    $"ZIP package exceeds the {MaxZipFileCount} supported files limit.");
            }

            var safeEntryFileName = Path.GetFileName(entryFileName);

            await using var entryStream = entry.Open();

            var storedEntryPath = await _fileStorageService.SaveAsync(
                safeEntryFileName,
                entryStream,
                cancellationToken);

            AddSingleFile(
                uploadBatch,
                safeEntryFileName,
                storedEntryPath,
                NormalizeContentType("application/octet-stream", entryExtension),
                entry.Length);
        }

        zipContent.Position = 0;
    }

    private static bool IsDirectory(ZipArchiveEntry entry)
        => string.IsNullOrWhiteSpace(entry.Name);

    private static bool IsSafeZipEntryName(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            return false;

        var normalized = entryName.Replace('\\', '/');

        if (Path.IsPathRooted(normalized))
            return false;

        var segments = normalized.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.All(segment =>
            segment != "." &&
            segment != ".." &&
            !segment.Contains(':'));
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