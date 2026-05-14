using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAI.Application.Abstractions.Services;
using OAI.Application.Uploads.Pdf;
using OAI.Infrastructure.Options;

namespace OAI.Infrastructure.Uploads.Pdf;

public sealed class PdfPageOcrService : IPdfPageOcrService
{
    private readonly IOcrService _ocrService;
    private readonly FileStorageOptions _options;
    private readonly string _basePath;
    private readonly ILogger<PdfPageOcrService> _logger;

    public PdfPageOcrService(
        IOcrService ocrService,
        IOptions<FileStorageOptions> options,
        ILogger<PdfPageOcrService> logger)
    {
        _ocrService = ocrService;
        _options = options.Value;
        _basePath = GetBasePath(_options.BasePath);
        _logger = logger;
    }

    public async Task<PdfPageOcrResult> OcrAsync(
        IReadOnlyList<PdfStoredPagePreviewResult> previews,
        CancellationToken cancellationToken = default)
    {
        if (previews is null || previews.Count == 0)
        {
            return PdfPageOcrResult.Failed("PDF page OCR failed because there are no stored page previews to process.");
        }

        var pageResults = new List<PdfPageOcrTextResult>(previews.Count);
        var warnings = new List<string>();

        foreach (var preview in previews.OrderBy(x => x.PageNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var previewPath = ResolvePreviewPath(preview.PreviewFilePath);
            if (previewPath is null || !File.Exists(previewPath))
            {
                var message = $"PDF page OCR skipped page {preview.PageNumber} because the preview image was not found.";
                warnings.Add(message);
                pageResults.Add(CreateEmptyPageResult(preview.PageNumber));
                _logger.LogWarning(
                    "PDF page OCR preview image was not found. PageNumber: {PageNumber}, PreviewFilePath: {PreviewFilePath}",
                    preview.PageNumber,
                    preview.PreviewFilePath);
                continue;
            }

            try
            {
                await using var stream = new FileStream(
                    previewPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

                var ocrResult = await _ocrService.ExtractTextAsync(
                    stream,
                    Path.GetFileName(previewPath),
                    cancellationToken);

                if (!ocrResult.IsSuccess)
                {
                    var message = $"PDF page OCR failed for page {preview.PageNumber}: {ocrResult.ErrorMessage ?? "OCR returned an unsuccessful result."}";
                    warnings.Add(message);
                    pageResults.Add(CreateEmptyPageResult(preview.PageNumber));
                    continue;
                }

                var rawText = NormalizeLineEndings(ocrResult.Text).Trim();
                pageResults.Add(new PdfPageOcrTextResult(
                    preview.PageNumber,
                    rawText,
                    GetConfidence(ocrResult.Confidence),
                    rawText.Length));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var message = $"PDF page OCR failed for page {preview.PageNumber}: {ex.Message}";
                warnings.Add(message);
                pageResults.Add(CreateEmptyPageResult(preview.PageNumber));

                _logger.LogWarning(
                    ex,
                    "PDF page OCR failed for one page. PageNumber: {PageNumber}, PreviewFilePath: {PreviewFilePath}",
                    preview.PageNumber,
                    preview.PreviewFilePath);
            }
        }

        var mergedText = MergePageText(pageResults);
        if (pageResults.All(x => x.CharacterCount == 0) || string.IsNullOrWhiteSpace(mergedText))
        {
            return new PdfPageOcrResult(
                Succeeded: false,
                MergedRawText: mergedText,
                Pages: pageResults,
                AverageConfidence: null,
                WarningMessage: BuildWarningMessage(warnings),
                ErrorMessage: "PDF page OCR failed because no text could be extracted from the stored page previews.");
        }

        var confidenceValues = pageResults
            .Select(x => x.Confidence)
            .OfType<decimal>()
            .ToList();

        return new PdfPageOcrResult(
            Succeeded: true,
            MergedRawText: mergedText,
            Pages: pageResults,
            AverageConfidence: confidenceValues.Count == 0 ? null : confidenceValues.Average(),
            WarningMessage: BuildWarningMessage(warnings),
            ErrorMessage: null);
    }

    private string? ResolvePreviewPath(string previewFilePath)
    {
        if (string.IsNullOrWhiteSpace(previewFilePath))
        {
            return null;
        }

        var normalizedPath = previewFilePath.Replace('\\', Path.DirectorySeparatorChar);
        return Path.IsPathRooted(normalizedPath)
            ? Path.GetFullPath(normalizedPath)
            : Path.GetFullPath(Path.Combine(_basePath, normalizedPath));
    }

    private static PdfPageOcrTextResult CreateEmptyPageResult(int pageNumber)
        => new(pageNumber, string.Empty, Confidence: null, CharacterCount: 0);

    private static string MergePageText(IReadOnlyList<PdfPageOcrTextResult> pages)
    {
        var builder = new StringBuilder();

        foreach (var page in pages.OrderBy(x => x.PageNumber))
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
                builder.Append('\n');
            }

            builder.Append($"--- Page {page.PageNumber} ---");
            builder.Append('\n');
            builder.Append(page.RawText.Trim());
        }

        return builder.ToString().Trim();
    }

    private static string NormalizeLineEndings(string? text)
        => (text ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n");

    private static decimal? GetConfidence(float confidence)
        => confidence > 0 ? (decimal)confidence : null;

    private static string? BuildWarningMessage(IReadOnlyList<string> warnings)
        => warnings.Count == 0 ? null : string.Join(" ", warnings);

    private static string GetBasePath(string? configuredBasePath)
        => !string.IsNullOrWhiteSpace(configuredBasePath)
            ? Path.GetFullPath(configuredBasePath)
            : AppContext.BaseDirectory;
}
