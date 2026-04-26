using Microsoft.Extensions.Options;
using OAI.Application.Abstractions.Services;
using OAI.Application.Ocr.Dtos;
using OAI.Infrastructure.Options;
using Tesseract;

namespace OAI.Infrastructure.Services;

public sealed class TesseractOcrService : IOcrService
{
    private readonly OcrOptions _options;

    public TesseractOcrService(IOptions<OcrOptions> options)
    {
        _options = options.Value;
    }

    public async Task<OcrResultDto> ExtractTextAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
            throw new ArgumentException("OCR content stream is invalid.", nameof(content));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // Phase 1: This wrapper processes the image first.
        // The PDF will have a separate converter added in a later step.
        if (extension == ".pdf")
        {
            return new OcrResultDto
            {
                IsSuccess = false,
                SourceFileName = fileName,
                ErrorMessage = "PDF OCR is not supported by Tesseract wrapper in this version."
            };
        }

        var imageBytes = await ReadAllBytesAsync(content, cancellationToken);

        return await Task.Run(() =>
        {
            var tessDataPath = GetAbsolutePath(_options.TessDataPath);
            if (!Directory.Exists(tessDataPath))
                throw new DirectoryNotFoundException($"Tessdata folder not found: {tessDataPath}");

            using var engine = new TesseractEngine(
                tessDataPath,
                _options.Languages,
                EngineMode.Default);

            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);

            var text = page.GetText()?.Trim() ?? string.Empty;
            var confidence = page.GetMeanConfidence();

            var lines = text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            return new OcrResultDto
            {
                IsSuccess = true,
                SourceFileName = fileName,
                Text = text,
                Confidence = confidence,
                Lines = lines
            };
        }, cancellationToken);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    private string GetAbsolutePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        var basePath = !string.IsNullOrWhiteSpace(_options.BasePath)
            ? _options.BasePath
            : AppContext.BaseDirectory;

        return Path.GetFullPath(Path.Combine(basePath, path));
    }
}