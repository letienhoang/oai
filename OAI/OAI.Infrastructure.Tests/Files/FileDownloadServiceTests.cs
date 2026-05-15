using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OAI.Application.Files;
using OAI.Domain.Entities;
using OAI.Domain.ValueObjects;
using OAI.Infrastructure.Files;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.Tests.Files;

public sealed class FileDownloadServiceTests
{
    [Fact]
    public async Task GetDownloadableFileAsync_ReturnsMetadataWhenSourceFileAndPhysicalFileExist()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, "storage/invoices/invoice.pdf", [0x01, 0x02]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, "invoice.pdf", "application/pdf", 2);
            var service = CreateService(dbContext, root);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal(Path.GetFullPath(Path.Combine(root, storedPath)), result.PhysicalPath);
            Assert.Equal("invoice.pdf", result.FileName);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.Equal(2, result.FileSizeBytes);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetDownloadableFileAsync_ReturnsNotFoundWhenMetadataDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, CreateTempDirectory());

        var result = await service.GetDownloadableFileAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(DownloadableFileErrorCode.NotFound, result.ErrorCode);
        Assert.Null(result.PhysicalPath);
    }

    [Fact]
    public async Task GetDownloadableFileAsync_ReturnsPhysicalFileMissingWhenFileDoesNotExist()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var sourceFile = await AddSourceFileAsync(
                dbContext,
                "storage/invoices/missing.pdf",
                "invoice.pdf",
                "application/pdf",
                123);
            var service = CreateService(dbContext, root);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.False(result.Succeeded);
            Assert.Equal(DownloadableFileErrorCode.PhysicalFileMissing, result.ErrorCode);
            Assert.Null(result.PhysicalPath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetDownloadableFileAsync_LogsStorageDetailsWhenPhysicalFileIsMissing()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var sourceFile = await AddSourceFileAsync(
                dbContext,
                "storage/invoices/missing.pdf",
                "invoice.pdf",
                "application/pdf",
                123);
            var logger = new CapturingLogger<FileDownloadService>();
            var service = CreateService(dbContext, root, logger: logger);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.False(result.Succeeded);
            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.LogLevel == LogLevel.Warning &&
                    entry.Message.Contains(sourceFile.Id.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    entry.Message.Contains("storage/invoices/missing.pdf", StringComparison.Ordinal) &&
                    entry.Message.Contains(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) &&
                    entry.Message.Contains("storage", StringComparison.Ordinal));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }


    [Fact]
    public async Task GetDownloadableFileAsync_RejectsUnsafePathTraversal()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var sourceFile = await AddSourceFileAsync(
                dbContext,
                "../outside.pdf",
                "outside.pdf",
                "application/pdf",
                123);
            var service = CreateService(dbContext, root);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.False(result.Succeeded);
            Assert.Equal(DownloadableFileErrorCode.UnsafePath, result.ErrorCode);
            Assert.Null(result.PhysicalPath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetDownloadableFileAsync_UsesStoredContentTypeWhenAvailable()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, "storage/invoices/document.bin", [0x01]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, "document.bin", "application/custom", 1);
            var service = CreateService(dbContext, root);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal("application/custom", result.ContentType);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetDownloadableFileAsync_FallsBackToContentTypeBasedOnExtension()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, "storage/invoices/page.png", [0x01]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, "page.png", "image/placeholder", 1);
            ClearContentType(dbContext, sourceFile);
            var service = CreateService(dbContext, root);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal("image/png", result.ContentType);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetDownloadableFileAsync_FallsBackToOctetStreamForUnknownExtension()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, "storage/invoices/file.unknown", [0x01]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, "file.unknown", "application/placeholder", 1);
            ClearContentType(dbContext, sourceFile);
            var service = CreateService(dbContext, root);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal("application/octet-stream", result.ContentType);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetDownloadableFileAsync_UsesOriginalFileNameWhenSafe()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, "storage/invoices/stored.pdf", [0x01]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, "friendly.pdf", "application/pdf", 1);
            var service = CreateService(dbContext, root);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal("friendly.pdf", result.FileName);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetDownloadableFileAsync_FallsBackToPhysicalFileNameWhenOriginalFileNameIsEmpty()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, "storage/invoices/stored.pdf", [0x01]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, "friendly.pdf", "application/pdf", 1);
            ClearOriginalFileName(dbContext, sourceFile);
            var service = CreateService(dbContext, root);

            var result = await service.GetDownloadableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal("stored.pdf", result.FileName);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Theory]
    [InlineData("storage/invoices/file.png", "image/png")]
    [InlineData("storage/invoices/file.jpg", "image/jpeg")]
    [InlineData("storage/invoices/file.pdf", "application/pdf")]
    [InlineData("storage/invoices/file.tiff", "image/tiff")]
    public async Task GetPreviewableFileAsync_ReturnsPreviewableFileForSupportedExtensions(
        string relativePath,
        string expectedContentType)
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, relativePath, [0x01, 0x02, 0x03]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, Path.GetFileName(relativePath), "application/placeholder", 3);
            ClearContentType(dbContext, sourceFile);
            var service = CreateService(dbContext, root);

            var result = await service.GetPreviewableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal(expectedContentType, result.ContentType);
            Assert.Equal(3, result.FileSizeBytes);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetPreviewableFileAsync_UsesPreviewFilePathWhenAvailable()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var previewPath = CreateStoredFile(root, "storage/invoices/previews/page-001.png", [0x01, 0x02]);
            var sourceFile = await AddSourceFileAsync(
                dbContext,
                "storage/invoices/original.pdf",
                "invoice.pdf",
                "image/png",
                100);
            SetPreview(dbContext, sourceFile, previewPath, 1);
            var service = CreateService(dbContext, root);

            var result = await service.GetPreviewableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal(Path.GetFullPath(Path.Combine(root, previewPath)), result.PhysicalPath);
            Assert.Equal("page-001.png", result.FileName);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal(2, result.FileSizeBytes);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetPreviewableFileAsync_UsesStoredContentTypeWhenPreviewable()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, "storage/invoices/document.bin", [0x01]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, "document.bin", "application/pdf", 1);
            var service = CreateService(dbContext, root);

            var result = await service.GetPreviewableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal("application/pdf", result.ContentType);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetPreviewableFileAsync_NormalizesJpgContentTypeToJpeg()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, "storage/invoices/photo.bin", [0x01]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, "photo.jpg", "image/jpg", 1);
            var service = CreateService(dbContext, root);

            var result = await service.GetPreviewableFileAsync(sourceFile.Id);

            Assert.True(result.Succeeded);
            Assert.Equal("image/jpeg", result.ContentType);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Theory]
    [InlineData("storage/invoices/archive.zip", "application/zip")]
    [InlineData("storage/invoices/file.bin", "application/placeholder")]
    public async Task GetPreviewableFileAsync_RejectsUnsupportedContentTypes(
        string relativePath,
        string storedContentType)
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var storedPath = CreateStoredFile(root, relativePath, [0x01]);
            var sourceFile = await AddSourceFileAsync(dbContext, storedPath, Path.GetFileName(relativePath), storedContentType, 1);
            var service = CreateService(dbContext, root);

            var result = await service.GetPreviewableFileAsync(sourceFile.Id);

            Assert.False(result.Succeeded);
            Assert.Equal(DownloadableFileErrorCode.UnsupportedContentType, result.ErrorCode);
            Assert.Null(result.PhysicalPath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetPreviewableFileAsync_ReturnsNotFoundWhenMetadataDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, CreateTempDirectory());

        var result = await service.GetPreviewableFileAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(DownloadableFileErrorCode.NotFound, result.ErrorCode);
        Assert.Null(result.PhysicalPath);
    }

    [Fact]
    public async Task GetPreviewableFileAsync_ReturnsPhysicalFileMissingWhenFileDoesNotExist()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var sourceFile = await AddSourceFileAsync(
                dbContext,
                "storage/invoices/missing.png",
                "missing.png",
                "image/png",
                123);
            var service = CreateService(dbContext, root);

            var result = await service.GetPreviewableFileAsync(sourceFile.Id);

            Assert.False(result.Succeeded);
            Assert.Equal(DownloadableFileErrorCode.PhysicalFileMissing, result.ErrorCode);
            Assert.Null(result.PhysicalPath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task GetPreviewableFileAsync_RejectsUnsafePathTraversal()
    {
        var root = CreateTempDirectory();

        try
        {
            await using var dbContext = CreateDbContext();
            var sourceFile = await AddSourceFileAsync(
                dbContext,
                "../outside.png",
                "outside.png",
                "image/png",
                123);
            var service = CreateService(dbContext, root);

            var result = await service.GetPreviewableFileAsync(sourceFile.Id);

            Assert.False(result.Succeeded);
            Assert.Equal(DownloadableFileErrorCode.UnsafePath, result.ErrorCode);
            Assert.Null(result.PhysicalPath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static FileDownloadService CreateService(
        OaiDbContext dbContext,
        string basePath,
        string rootPath = "storage",
        ILogger<FileDownloadService>? logger = null)
        => new(
            dbContext,
            Microsoft.Extensions.Options.Options.Create(new FileStorageOptions
            {
                BasePath = basePath,
                RootPath = rootPath,
                InvoiceFolder = "invoices"
            }),
            logger ?? NullLogger<FileDownloadService>.Instance);

    private static OaiDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OaiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new OaiDbContext(options);
    }

    private static async Task<InvoiceSourceFile> AddSourceFileAsync(
        OaiDbContext dbContext,
        string storedPath,
        string originalFileName,
        string contentType,
        long fileSizeBytes)
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            "INV-001",
            DateOnly.FromDateTime(DateTime.UtcNow),
            "USD",
            new Money(1, "USD"),
            Money.Zero("USD"),
            new Money(1, "USD"));

        var sourceFile = new InvoiceSourceFile(
            invoice.Id,
            originalFileName,
            storedPath,
            contentType,
            fileSizeBytes);

        invoice.AddSourceFile(sourceFile);
        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        return sourceFile;
    }

    private static void ClearContentType(
        OaiDbContext dbContext,
        InvoiceSourceFile sourceFile)
    {
        dbContext.Entry(sourceFile).Property(x => x.ContentType).CurrentValue = string.Empty;
        dbContext.SaveChanges();
    }

    private static void ClearOriginalFileName(
        OaiDbContext dbContext,
        InvoiceSourceFile sourceFile)
    {
        dbContext.Entry(sourceFile).Property(x => x.OriginalFileName).CurrentValue = string.Empty;
        dbContext.SaveChanges();
    }

    private static void SetPreview(
        OaiDbContext dbContext,
        InvoiceSourceFile sourceFile,
        string previewPath,
        int pageNumber)
    {
        sourceFile.UpdatePreview(previewPath, pageNumber);
        dbContext.SaveChanges();
    }

    private static string CreateStoredFile(
        string root,
        string relativePath,
        byte[] bytes)
    {
        var physicalPath = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        File.WriteAllBytes(physicalPath, bytes);

        return relativePath.Replace('\\', '/');
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "oai-file-download-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
