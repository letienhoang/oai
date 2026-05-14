using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OAI.Application.Abstractions.Services;
using OAI.Application.Ocr.Dtos;
using OAI.Application.Uploads.Pdf;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Uploads.Pdf;

namespace OAI.Infrastructure.Tests.Uploads.Pdf;

public sealed class PdfPageOcrServiceTests
{
    [Fact]
    public async Task OcrAsync_OcrsMultiplePagePreviewsInPageOrderAndMergesText()
    {
        var root = CreateTempDirectory();

        try
        {
            CreatePreview(root, "page-001.png");
            CreatePreview(root, "page-002.png");

            var ocr = new FakeOcrService(
                ("page-001.png", Successful("First page text", 0.80f)),
                ("page-002.png", Successful("Second page text", 0.90f)));
            var service = CreateService(root, ocr);

            var result = await service.OcrAsync(
                [
                    Preview(2, "storage/page-002.png"),
                    Preview(1, "storage/page-001.png")
                ]);

            Assert.True(result.Succeeded);
            Assert.Equal(
                "--- Page 1 ---\nFirst page text\n\n--- Page 2 ---\nSecond page text",
                result.MergedRawText);
            Assert.Collection(
                result.Pages,
                page => Assert.Equal(1, page.PageNumber),
                page => Assert.Equal(2, page.PageNumber));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task OcrAsync_ComputesAverageConfidenceWhenAvailable()
    {
        var root = CreateTempDirectory();

        try
        {
            CreatePreview(root, "page-001.png");
            CreatePreview(root, "page-002.png");

            var service = CreateService(
                root,
                new FakeOcrService(
                    ("page-001.png", Successful("A", 0.70f)),
                    ("page-002.png", Successful("B", 0.90f))));

            var result = await service.OcrAsync(
                [
                    Preview(1, "storage/page-001.png"),
                    Preview(2, "storage/page-002.png")
                ]);

            Assert.True(result.Succeeded);
            Assert.Equal(0.80m, Math.Round(result.AverageConfidence!.Value, 2));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task OcrAsync_ReturnsNullAverageConfidenceWhenConfidenceIsUnavailable()
    {
        var root = CreateTempDirectory();

        try
        {
            CreatePreview(root, "page-001.png");
            var service = CreateService(
                root,
                new FakeOcrService(("page-001.png", Successful("A", 0f))));

            var result = await service.OcrAsync([Preview(1, "storage/page-001.png")]);

            Assert.True(result.Succeeded);
            Assert.Null(result.AverageConfidence);
            Assert.Null(result.Pages[0].Confidence);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task OcrAsync_NormalizesLineEndings()
    {
        var root = CreateTempDirectory();

        try
        {
            CreatePreview(root, "page-001.png");
            var service = CreateService(
                root,
                new FakeOcrService(("page-001.png", Successful("Line 1\r\nLine 2\rLine 3", 0.75f))));

            var result = await service.OcrAsync([Preview(1, "storage/page-001.png")]);

            Assert.True(result.Succeeded);
            Assert.Equal("--- Page 1 ---\nLine 1\nLine 2\nLine 3", result.MergedRawText);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task OcrAsync_HandlesOneFailedPageWithoutCrashing()
    {
        var root = CreateTempDirectory();

        try
        {
            CreatePreview(root, "page-001.png");
            CreatePreview(root, "page-002.png");
            var service = CreateService(
                root,
                new FakeOcrService(
                    ("page-001.png", Failed("bad page")),
                    ("page-002.png", Successful("Good page", 0.88f))));

            var result = await service.OcrAsync(
                [
                    Preview(1, "storage/page-001.png"),
                    Preview(2, "storage/page-002.png")
                ]);

            Assert.True(result.Succeeded);
            Assert.Contains("PDF page OCR failed for page 1", result.WarningMessage);
            Assert.Equal(0, result.Pages[0].CharacterCount);
            Assert.Contains("--- Page 2 ---\nGood page", result.MergedRawText);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task OcrAsync_FailsClearlyWhenAllPreviewFilesAreMissing()
    {
        var root = CreateTempDirectory();

        try
        {
            var service = CreateService(root, new FakeOcrService());

            var result = await service.OcrAsync(
                [
                    Preview(1, "storage/missing-001.png"),
                    Preview(2, "storage/missing-002.png")
                ]);

            Assert.False(result.Succeeded);
            Assert.Contains("no text could be extracted", result.ErrorMessage);
            Assert.Equal(2, result.Pages.Count);
            Assert.All(result.Pages, page => Assert.Equal(0, page.CharacterCount));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static PdfPageOcrService CreateService(
        string basePath,
        IOcrService ocrService)
        => new(
            ocrService,
            Microsoft.Extensions.Options.Options.Create(new FileStorageOptions
            {
                BasePath = basePath,
                RootPath = "storage",
                InvoiceFolder = "invoices"
            }),
            NullLogger<PdfPageOcrService>.Instance);

    private static PdfStoredPagePreviewResult Preview(
        int pageNumber,
        string previewFilePath)
        => new(pageNumber, previewFilePath, "image/png", 1);

    private static OcrResultDto Successful(string text, float confidence)
        => new()
        {
            IsSuccess = true,
            SourceFileName = "page.png",
            Text = text,
            Confidence = confidence
        };

    private static OcrResultDto Failed(string errorMessage)
        => new()
        {
            IsSuccess = false,
            SourceFileName = "page.png",
            ErrorMessage = errorMessage
        };

    private static void CreatePreview(string root, string fileName)
    {
        var directory = Path.Combine(root, "storage");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, fileName), [0x89, 0x50, 0x4e, 0x47]);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "oai-pdf-page-ocr-tests",
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

    private sealed class FakeOcrService : IOcrService
    {
        private readonly IReadOnlyDictionary<string, OcrResultDto> _results;

        public FakeOcrService(params (string FileName, OcrResultDto Result)[] results)
        {
            _results = results.ToDictionary(x => x.FileName, x => x.Result);
        }

        public Task<OcrResultDto> ExtractTextAsync(
            Stream content,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _results.TryGetValue(fileName, out var result)
                    ? result
                    : Failed($"No fake OCR result configured for {fileName}."));
        }
    }
}
