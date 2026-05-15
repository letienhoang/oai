using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAI.Application.Abstractions.Services;
using OAI.Infrastructure.Options;

namespace OAI.Infrastructure.Services;

public sealed class FileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly string _basePath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IOptions<FileStorageOptions> options,  ILogger<FileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _basePath = GetBasePath(_options.BasePath);

        if (string.IsNullOrWhiteSpace(_options.BasePath))
        {
            _logger.LogWarning(
                "FileStorage:BasePath is empty. Storage will be resolved relative to AppContext.BaseDirectory. This may break multi-process Web/API/Worker setups.");
        }

        _logger.LogInformation(
            "File storage base path resolved to {BasePath}. RootPath: {RootPath}",
            _basePath,
            _options.RootPath);
    }

    public async Task<string> SaveAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving file {FileName}", fileName);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        if (content is null || !content.CanRead)
            throw new ArgumentException("File stream is invalid.", nameof(content));

        if (content.CanSeek && content.Length > _options.MaxFileSizeBytes)
            throw new InvalidOperationException($"File size exceeds limit of {_options.MaxFileSizeBytes} bytes.");

        var safeFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";

        var relativeFolder = Path.Combine(
            _options.RootPath,
            _options.InvoiceFolder,
            DateTime.UtcNow.Year.ToString("D4"),
            DateTime.UtcNow.Month.ToString("D2"));

        var physicalFolder = Path.Combine(_basePath, relativeFolder);
        Directory.CreateDirectory(physicalFolder);

        var physicalPath = Path.Combine(physicalFolder, storedFileName);

        await using (var fileStream = new FileStream(
            physicalPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var relativePath = Path.Combine(relativeFolder, storedFileName);
        
        _logger.LogInformation("File {FileName} saved to {RelativePath}", fileName, relativePath);
        return NormalizePath(relativePath);
    }

    public Task<Stream?> OpenReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult<Stream?>(null);

        var physicalPath = GetPhysicalPath(path);

        if (!File.Exists(physicalPath))
        {
            _logger.LogWarning("File not found at {PhysicalPath}", physicalPath);
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting file at {Path}", path);
        if (string.IsNullOrWhiteSpace(path))
            return Task.CompletedTask;

        var physicalPath = GetPhysicalPath(path);

        if (File.Exists(physicalPath))
            File.Delete(physicalPath);

        return Task.CompletedTask;
    }

    public string GetPhysicalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return _basePath;

        var normalized = path.Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized))
            return Path.GetFullPath(normalized);

        return Path.GetFullPath(Path.Combine(_basePath, normalized));
    }

    public string GetStorageRootPhysicalPath()
        => GetPhysicalPath(_options.RootPath);

    private static string NormalizePath(string path)
        => path.Replace('\\', '/');

    private static string GetBasePath(string? configuredBasePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredBasePath))
            return Path.GetFullPath(configuredBasePath);

        return AppContext.BaseDirectory;
    }
}
