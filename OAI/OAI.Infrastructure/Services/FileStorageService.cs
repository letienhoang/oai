using Microsoft.Extensions.Options;
using OAI.Application.Abstractions.Services;
using OAI.Infrastructure.Options;

namespace OAI.Infrastructure.Services;

public sealed class FileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly string _basePath;

    public FileStorageService(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
        _basePath = GetBasePath(_options.BasePath);
    }

    public async Task<string> SaveAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
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
            return Task.FromResult<Stream?>(null);

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
        if (string.IsNullOrWhiteSpace(path))
            return Task.CompletedTask;

        var physicalPath = GetPhysicalPath(path);

        if (File.Exists(physicalPath))
            File.Delete(physicalPath);

        return Task.CompletedTask;
    }

    private string GetPhysicalPath(string path)
    {
        var normalized = path.Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized))
            return normalized;

        return Path.Combine(_basePath, normalized);
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/');

    private static string GetBasePath(string? configuredBasePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredBasePath))
            return Path.GetFullPath(configuredBasePath);

        return AppContext.BaseDirectory;
    }
}