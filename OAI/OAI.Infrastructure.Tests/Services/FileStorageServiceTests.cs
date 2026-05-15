using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Services;

namespace OAI.Infrastructure.Tests.Services;

public sealed class FileStorageServiceTests
{
    [Fact]
    public void GetPhysicalPath_ResolvesRelativePathAgainstConfiguredBasePath()
    {
        var root = CreateTempDirectory();

        try
        {
            var service = CreateService(new FileStorageOptions
            {
                BasePath = root,
                RootPath = "storage",
                InvoiceFolder = "invoices"
            });

            var physicalPath = service.GetPhysicalPath("storage/invoices/2026/05/invoice.png");

            Assert.Equal(
                Path.GetFullPath(Path.Combine(root, "storage", "invoices", "2026", "05", "invoice.png")),
                physicalPath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void GetStorageRootPhysicalPath_UsesConfiguredBasePathAndRootPath()
    {
        var root = CreateTempDirectory();

        try
        {
            var service = CreateService(new FileStorageOptions
            {
                BasePath = root,
                RootPath = "storage"
            });

            var physicalRoot = service.GetStorageRootPhysicalPath();

            Assert.Equal(Path.GetFullPath(Path.Combine(root, "storage")), physicalRoot);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Constructor_LogsWarningWhenBasePathIsEmpty()
    {
        var logger = new CapturingLogger<FileStorageService>();

        _ = new FileStorageService(
            Microsoft.Extensions.Options.Options.Create(new FileStorageOptions
            {
                BasePath = "",
                RootPath = "storage"
            }),
            logger);

        Assert.Contains(
            logger.Entries,
            entry =>
                entry.LogLevel == LogLevel.Warning &&
                entry.Message.Contains("FileStorage:BasePath is empty", StringComparison.Ordinal));
    }

    private static FileStorageService CreateService(FileStorageOptions options)
        => new(
            Microsoft.Extensions.Options.Options.Create(options),
            new CapturingLogger<FileStorageService>());

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "oai-filestorage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
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
