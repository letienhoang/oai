using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAI.Application.Files;
using OAI.Domain.Entities;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Files;

public sealed class FileDownloadService : IFileDownloadService
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".zip"] = "application/zip"
        };

    private readonly OaiDbContext _dbContext;
    private readonly FileStorageOptions _options;
    private readonly string _basePath;
    private readonly ILogger<FileDownloadService> _logger;

    public FileDownloadService(
        OaiDbContext dbContext,
        IOptions<FileStorageOptions> options,
        ILogger<FileDownloadService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _basePath = GetBasePath(_options.BasePath);
        _logger = logger;
    }

    public async Task<DownloadableFileResult> GetDownloadableFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        if (fileId == Guid.Empty)
        {
            return Failed(
                DownloadableFileErrorCode.NotFound,
                "File was not found.");
        }

        var storageRootPath = NormalizeConfiguredRootPath(_options.RootPath);
        if (storageRootPath is null)
        {
            _logger.LogError(
                "File download failed because configured storage root path is unsafe. RootPath: {RootPath}",
                _options.RootPath);

            return Failed(
                DownloadableFileErrorCode.UnsafePath,
                "File path is not available.");
        }

        var sourceFile = await _dbContext.InvoiceSourceFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == fileId, cancellationToken);

        if (sourceFile is null)
        {
            return Failed(
                DownloadableFileErrorCode.NotFound,
                "File was not found.");
        }

        var storedPath = GetDownloadPath(sourceFile);
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return Failed(
                DownloadableFileErrorCode.Unsupported,
                "File path is not available.");
        }

        var storagePhysicalRoot = Path.GetFullPath(Path.Combine(_basePath, storageRootPath));
        var physicalPath = ResolvePhysicalPath(storedPath, storagePhysicalRoot);

        if (physicalPath is null || !IsUnderRoot(physicalPath, storagePhysicalRoot))
        {
            _logger.LogWarning(
                "Rejected unsafe invoice source file path. FileId: {FileId}, StoredPath: {StoredPath}, StorageRoot: {StorageRoot}",
                fileId,
                storedPath,
                storagePhysicalRoot);

            return Failed(
                DownloadableFileErrorCode.UnsafePath,
                "File path is not available.");
        }

        if (!File.Exists(physicalPath))
        {
            _logger.LogWarning(
                "Invoice source file metadata exists but physical file is missing. FileId: {FileId}, PhysicalPath: {PhysicalPath}",
                fileId,
                physicalPath);

            return Failed(
                DownloadableFileErrorCode.PhysicalFileMissing,
                "File was not found.");
        }

        var fileInfo = new FileInfo(physicalPath);
        var fileName = GetDownloadFileName(sourceFile, physicalPath, storedPath);
        var contentType = GetContentType(sourceFile.ContentType, physicalPath);

        return new DownloadableFileResult(
            Succeeded: true,
            PhysicalPath: physicalPath,
            FileName: fileName,
            ContentType: contentType,
            FileSizeBytes: fileInfo.Length,
            ErrorMessage: null,
            ErrorCode: null);
    }

    private string? ResolvePhysicalPath(string storedPath, string storagePhysicalRoot)
    {
        var normalizedPath = storedPath.Trim().Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalizedPath))
        {
            return Path.GetFullPath(normalizedPath);
        }

        var baseRelativePath = Path.GetFullPath(Path.Combine(_basePath, normalizedPath));
        if (IsUnderRoot(baseRelativePath, storagePhysicalRoot))
        {
            return baseRelativePath;
        }

        var rootRelativePath = Path.GetFullPath(Path.Combine(storagePhysicalRoot, normalizedPath));
        return IsUnderRoot(rootRelativePath, storagePhysicalRoot)
            ? rootRelativePath
            : null;
    }

    private static string? GetDownloadPath(InvoiceSourceFile sourceFile)
    {
        if (!string.IsNullOrWhiteSpace(sourceFile.StoredFilePath))
        {
            return sourceFile.StoredFilePath;
        }

        return string.IsNullOrWhiteSpace(sourceFile.PreviewFilePath)
            ? null
            : sourceFile.PreviewFilePath;
    }

    private static string GetDownloadFileName(
        InvoiceSourceFile sourceFile,
        string physicalPath,
        string storedPath)
    {
        if (TryGetSafeFileName(sourceFile.OriginalFileName, out var originalFileName))
        {
            return originalFileName;
        }

        if (!string.IsNullOrWhiteSpace(sourceFile.PreviewFilePath) &&
            string.Equals(sourceFile.PreviewFilePath, storedPath, StringComparison.OrdinalIgnoreCase) &&
            sourceFile.PageNumber is > 0)
        {
            return $"page-{sourceFile.PageNumber.Value:000}{Path.GetExtension(physicalPath)}";
        }

        return TryGetSafeFileName(Path.GetFileName(physicalPath), out var physicalFileName)
            ? physicalFileName
            : "download";
    }

    private static bool TryGetSafeFileName(string? fileName, out string safeFileName)
    {
        safeFileName = string.Empty;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var candidate = fileName.Trim()
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();
        if (string.IsNullOrWhiteSpace(candidate) ||
            candidate is "." or ".." ||
            candidate.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return false;
        }

        safeFileName = candidate;
        return true;
    }

    private static string GetContentType(string? storedContentType, string physicalPath)
    {
        if (!string.IsNullOrWhiteSpace(storedContentType))
        {
            return storedContentType.Trim();
        }

        var extension = Path.GetExtension(physicalPath);
        return ContentTypesByExtension.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static DownloadableFileResult Failed(
        DownloadableFileErrorCode errorCode,
        string message)
        => new(
            Succeeded: false,
            PhysicalPath: null,
            FileName: null,
            ContentType: null,
            FileSizeBytes: null,
            ErrorMessage: message,
            ErrorCode: errorCode);

    private static string GetBasePath(string? configuredBasePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredBasePath))
        {
            return Path.GetFullPath(configuredBasePath);
        }

        return AppContext.BaseDirectory;
    }

    private static string? NormalizeConfiguredRootPath(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return "storage";
        }

        var normalized = rootPath.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized) || Path.IsPathRooted(normalized))
        {
            return null;
        }

        var segments = normalized.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Any(segment =>
                segment == "." ||
                segment == ".." ||
                segment.Contains(':') ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            return null;
        }

        return Path.Combine(segments);
    }

    private static bool IsUnderRoot(string path, string rootPath)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(rootPath);

        if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            fullRoot += Path.DirectorySeparatorChar;
        }

        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }
}
