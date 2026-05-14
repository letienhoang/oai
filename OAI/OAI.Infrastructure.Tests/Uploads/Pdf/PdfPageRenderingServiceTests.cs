using Microsoft.Extensions.Logging.Abstractions;
using OAI.Application.Uploads.Pdf;
using OAI.Infrastructure.Uploads.Pdf;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace OAI.Infrastructure.Tests.Uploads.Pdf;

public sealed class PdfPageRenderingServiceTests
{
    private readonly PdfPageRenderingService _service = new(
        NullLogger<PdfPageRenderingService>.Instance);

    [Fact]
    public async Task RenderAsync_RendersOnePageScannedStylePdfToPngFile()
    {
        var outputDirectory = CreateTempDirectory();

        try
        {
            await using var stream = new MemoryStream(CreateBlankPdf(pageCount: 1));

            var result = await _service.RenderAsync(
                stream,
                CreateOptions(outputDirectory, "scan"));

            Assert.True(result.Succeeded);
            Assert.Equal(1, result.PageCount);
            var page = Assert.Single(result.Pages);
            Assert.Equal(1, page.PageNumber);
            Assert.Equal("image/png", page.ContentType);
            Assert.True(File.Exists(page.ImagePath));
            Assert.True(page.FileSizeBytes > 0);
            Assert.True(page.Width > 0);
            Assert.True(page.Height > 0);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task RenderAsync_RendersMultiPagePdfIntoMultiplePngFiles()
    {
        var outputDirectory = CreateTempDirectory();

        try
        {
            await using var stream = new MemoryStream(CreateBlankPdf(pageCount: 3));

            var result = await _service.RenderAsync(
                stream,
                CreateOptions(outputDirectory, "batch-file"));

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.PageCount);
            Assert.Equal(3, result.Pages.Count);
            Assert.All(result.Pages, page => Assert.True(File.Exists(page.ImagePath)));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task RenderAsync_RespectsMaxPagesLimit()
    {
        var outputDirectory = CreateTempDirectory();

        try
        {
            await using var stream = new MemoryStream(CreateBlankPdf(pageCount: 3));

            var result = await _service.RenderAsync(
                stream,
                CreateOptions(outputDirectory, "limited", maxPages: 2));

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.PageCount);
            Assert.Equal(2, result.Pages.Count);
            Assert.Equal(
                "The PDF has more pages than the configured rendering limit. Only the first 2 pages were rendered.",
                result.WarningMessage);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task RenderAsync_ReturnsFailedResultForInvalidPdfBytes()
    {
        var outputDirectory = CreateTempDirectory();

        try
        {
            await using var stream = new MemoryStream([0x01, 0x02, 0x03, 0x04]);

            var result = await _service.RenderAsync(
                stream,
                CreateOptions(outputDirectory, "broken"));

            Assert.False(result.Succeeded);
            Assert.Equal(0, result.PageCount);
            Assert.Empty(result.Pages);
            Assert.NotNull(result.ErrorMessage);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task RenderAsync_CreatesOutputDirectoryWhenItDoesNotExist()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "oai-pdf-rendering-tests", Guid.NewGuid().ToString("N"));

        try
        {
            await using var stream = new MemoryStream(CreateBlankPdf(pageCount: 1));

            var result = await _service.RenderAsync(
                stream,
                CreateOptions(outputDirectory, "created"));

            Assert.True(result.Succeeded);
            Assert.True(Directory.Exists(outputDirectory));
            Assert.True(File.Exists(result.Pages[0].ImagePath));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task RenderAsync_UsesPredictableFileNames()
    {
        var outputDirectory = CreateTempDirectory();

        try
        {
            await using var stream = new MemoryStream(CreateBlankPdf(pageCount: 2));

            var result = await _service.RenderAsync(
                stream,
                CreateOptions(outputDirectory, "invoice-abc"));

            Assert.True(result.Succeeded);
            Assert.Equal(
                Path.Combine(outputDirectory, "invoice-abc-page-001.png"),
                result.Pages[0].ImagePath);
            Assert.Equal(
                Path.Combine(outputDirectory, "invoice-abc-page-002.png"),
                result.Pages[1].ImagePath);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task RenderAsync_RestoresSeekableStreamPosition()
    {
        var outputDirectory = CreateTempDirectory();

        try
        {
            await using var stream = new MemoryStream(CreatePdf("tiny"));
            stream.Position = 5;

            var result = await _service.RenderAsync(
                stream,
                CreateOptions(outputDirectory, "position"));

            Assert.True(result.Succeeded);
            Assert.Equal(5, stream.Position);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    private static PdfPageRenderingOptions CreateOptions(
        string outputDirectory,
        string prefix,
        int maxPages = 20)
        => new()
        {
            OutputDirectory = outputDirectory,
            FileNamePrefix = prefix,
            Dpi = 100,
            MaxPages = maxPages,
            OverwriteExistingFiles = true
        };

    private static byte[] CreatePdf(params string?[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var pageText in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                page.AddText(pageText, 12, new PdfPoint(25, 780), font);
            }
        }

        return builder.Build();
    }

    private static byte[] CreateBlankPdf(int pageCount)
        => CreatePdf(Enumerable.Repeat<string?>(null, pageCount).ToArray());

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "oai-pdf-rendering-tests",
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
}
