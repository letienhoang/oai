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

    private static readonly ISet<string> PreviewableContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/png",
            "image/jpeg",
            "image/tiff"
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
        => await GetFileAsync(
            fileId,
            FileAccessMode.Download,
            cancellationToken);

    public async Task<DownloadableFileResult> GetPreviewableFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
        => await GetFileAsync(
            fileId,
            FileAccessMode.Preview,
            cancellationToken);

    private async Task<DownloadableFileResult> GetFileAsync(
        Guid fileId,
        FileAccessMode accessMode,
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
                "File access failed because configured storage root path is unsafe. RootPath: {RootPath}",
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

        var storedPath = GetAccessPath(sourceFile, accessMode);
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
        var fileName = GetAccessFileName(sourceFile, physicalPath, storedPath, accessMode);
        var contentType = accessMode == FileAccessMode.Preview
            ? GetPreviewContentType(sourceFile.ContentType, physicalPath)
            : GetDownloadContentType(sourceFile.ContentType, physicalPath);

        if (accessMode == FileAccessMode.Preview &&
            !PreviewableContentTypes.Contains(contentType))
        {
            return Failed(
                DownloadableFileErrorCode.UnsupportedContentType,
                "File type cannot be previewed.");
        }

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

    private static string? GetAccessPath(
        InvoiceSourceFile sourceFile,
        FileAccessMode accessMode)
    {
        if (accessMode == FileAccessMode.Preview &&
            !string.IsNullOrWhiteSpace(sourceFile.PreviewFilePath))
        {
            return sourceFile.PreviewFilePath;
        }

        return GetDownloadPath(sourceFile);
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

    private static string GetAccessFileName(
        InvoiceSourceFile sourceFile,
        string physicalPath,
        string storedPath,
        FileAccessMode accessMode)
    {
        if (accessMode == FileAccessMode.Preview &&
            TryGetPagePreviewFileName(sourceFile, physicalPath, storedPath, out var previewFileName))
        {
            return previewFileName;
        }

        return GetDownloadFileName(sourceFile, physicalPath, storedPath);
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

        if (TryGetPagePreviewFileName(sourceFile, physicalPath, storedPath, out var previewFileName))
        {
            return previewFileName;
        }

        return TryGetSafeFileName(Path.GetFileName(physicalPath), out var physicalFileName)
            ? physicalFileName
            : "download";
    }

    private static bool TryGetPagePreviewFileName(
        InvoiceSourceFile sourceFile,
        string physicalPath,
        string storedPath,
        out string fileName)
    {
        fileName = string.Empty;

        if (!string.IsNullOrWhiteSpace(sourceFile.PreviewFilePath) &&
            string.Equals(sourceFile.PreviewFilePath, storedPath, StringComparison.OrdinalIgnoreCase) &&
            sourceFile.PageNumber is > 0)
        {
            fileName = $"page-{sourceFile.PageNumber.Value:000}{Path.GetExtension(physicalPath)}";
            return true;
        }

        return false;
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

    private static string GetDownloadContentType(string? storedContentType, string physicalPath)
    {
        if (!string.IsNullOrWhiteSpace(storedContentType))
        {
            return storedContentType.Trim();
        }

        return GetContentTypeFromExtension(physicalPath) ?? "application/octet-stream";
    }

    private static string GetPreviewContentType(string? storedContentType, string physicalPath)
    {
        var normalizedStoredContentType = NormalizeContentType(storedContentType);
        if (!string.IsNullOrWhiteSpace(normalizedStoredContentType) &&
            PreviewableContentTypes.Contains(normalizedStoredContentType))
        {
            return normalizedStoredContentType;
        }

        var extensionContentType = GetContentTypeFromExtension(physicalPath);
        if (!string.IsNullOrWhiteSpace(extensionContentType))
        {
            return extensionContentType;
        }

        return string.IsNullOrWhiteSpace(normalizedStoredContentType)
            ? "application/octet-stream"
            : normalizedStoredContentType;
    }

    private static string? NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        var normalized = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        return normalized == "image/jpg"
            ? "image/jpeg"
            : normalized;
    }

    private static string? GetContentTypeFromExtension(string physicalPath)
    {
        var extension = Path.GetExtension(physicalPath);
        return ContentTypesByExtension.TryGetValue(extension, out var contentType)
            ? contentType
            : null;
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

    private enum FileAccessMode
    {
        Download,
        Preview
    }
}
