using Microsoft.Extensions.Logging;
using OAI.Application.Uploads.Pdf;
using PDFtoImage;
using PDFtoImage.Exceptions;
using SkiaSharp;

namespace OAI.Infrastructure.Uploads.Pdf;

public sealed class PdfPageRenderingService : IPdfPageRenderingService
{
    private const int MinimumDpi = 100;
    private const int MaximumDpi = 300;
    private const int MinimumMaxPages = 1;
    private const int MaximumMaxPages = 50;
    private const string PngContentType = "image/png";

    private readonly ILogger<PdfPageRenderingService> _logger;

    public PdfPageRenderingService(ILogger<PdfPageRenderingService> logger)
    {
        _logger = logger;
    }

    public async Task<PdfPageRenderingResult> RenderAsync(
        Stream pdfStream,
        PdfPageRenderingOptions options,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(pdfStream, options);
        if (validationError is not null)
        {
            return PdfPageRenderingResult.Failed(validationError);
        }

        var originalPosition = pdfStream.CanSeek ? pdfStream.Position : (long?)null;

        try
        {
            var renderingStream = await PrepareSeekableStreamAsync(pdfStream, cancellationToken);
            await using var copiedStream = ReferenceEquals(renderingStream, pdfStream)
                ? null
                : renderingStream;

            var dpi = Math.Clamp(options.Dpi, MinimumDpi, MaximumDpi);
            var maxPages = Math.Clamp(options.MaxPages, MinimumMaxPages, MaximumMaxPages);
            var fileNamePrefix = SanitizeFileNamePrefix(options.FileNamePrefix);

            Directory.CreateDirectory(options.OutputDirectory);

            renderingStream.Position = 0;
#pragma warning disable CA1416
            var pageCount = Conversion.GetPageCount(renderingStream, leaveOpen: true);
#pragma warning restore CA1416
            var pagesToRender = Math.Min(pageCount, maxPages);
            var renderedPages = new List<PdfRenderedPageResult>(pagesToRender);
            var renderOptions = new RenderOptions { Dpi = dpi };

            for (var pageNumber = 1; pageNumber <= pagesToRender; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var imagePath = Path.Combine(
                    options.OutputDirectory,
                    $"{fileNamePrefix}-page-{pageNumber:000}.png");

                if (File.Exists(imagePath) && !options.OverwriteExistingFiles)
                {
                    return PdfPageRenderingResult.Failed(
                        $"PDF page rendering failed because the output file already exists: {imagePath}");
                }

                renderingStream.Position = 0;
#pragma warning disable CA1416
                Conversion.SavePng(
                    imagePath,
                    renderingStream,
                    Index.FromStart(pageNumber - 1),
                    leaveOpen: true,
                    options: renderOptions);
#pragma warning restore CA1416

                var fileInfo = new FileInfo(imagePath);
                var (width, height) = ReadImageDimensions(imagePath);

                renderedPages.Add(new PdfRenderedPageResult(
                    PageNumber: pageNumber,
                    ImagePath: imagePath,
                    ContentType: PngContentType,
                    FileSizeBytes: fileInfo.Length,
                    Width: width,
                    Height: height));
            }

            var warningMessage = pageCount > maxPages
                ? $"The PDF has more pages than the configured rendering limit. Only the first {maxPages} pages were rendered."
                : null;

            return new PdfPageRenderingResult(
                Succeeded: true,
                PageCount: pageCount,
                Pages: renderedPages,
                WarningMessage: warningMessage,
                ErrorMessage: null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PdfException ex)
        {
            _logger.LogWarning(
                ex,
                "PDF page rendering failed because the PDF could not be rendered. OutputDirectory: {OutputDirectory}",
                options.OutputDirectory);

            return PdfPageRenderingResult.Failed("PDF page rendering failed because the PDF file is invalid or unreadable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PDF page rendering failed unexpectedly. OutputDirectory: {OutputDirectory}",
                options.OutputDirectory);

            return PdfPageRenderingResult.Failed("PDF page rendering failed unexpectedly.");
        }
        finally
        {
            if (originalPosition.HasValue && pdfStream.CanSeek)
            {
                pdfStream.Position = originalPosition.Value;
            }
        }
    }

    private static string? Validate(Stream pdfStream, PdfPageRenderingOptions options)
    {
        if (pdfStream is null || !pdfStream.CanRead)
        {
            return "PDF page rendering failed because the PDF stream is invalid.";
        }

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return "PDF page rendering failed because the output directory is required.";
        }

        if (string.IsNullOrWhiteSpace(options.FileNamePrefix))
        {
            return "PDF page rendering failed because the file name prefix is required.";
        }

        var sanitizedPrefix = SanitizeFileNamePrefix(options.FileNamePrefix);
        if (!string.Equals(options.FileNamePrefix, sanitizedPrefix, StringComparison.Ordinal))
        {
            return "PDF page rendering failed because the file name prefix contains invalid characters.";
        }

        if (options.Dpi is < MinimumDpi or > MaximumDpi)
        {
            return $"PDF page rendering failed because DPI must be between {MinimumDpi} and {MaximumDpi}.";
        }

        if (options.MaxPages is < MinimumMaxPages or > MaximumMaxPages)
        {
            return $"PDF page rendering failed because MaxPages must be between {MinimumMaxPages} and {MaximumMaxPages}.";
        }

        return null;
    }

    private static async Task<Stream> PrepareSeekableStreamAsync(
        Stream pdfStream,
        CancellationToken cancellationToken)
    {
        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
            return pdfStream;
        }

        var copy = new MemoryStream();
        await pdfStream.CopyToAsync(copy, cancellationToken);
        copy.Position = 0;
        return copy;
    }

    private static string SanitizeFileNamePrefix(string prefix)
    {
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        var sanitizedChars = prefix
            .Trim()
            .Select(ch => invalidFileNameChars.Contains(ch) ? '-' : ch)
            .ToArray();

        return new string(sanitizedChars);
    }

    private static (int? Width, int? Height) ReadImageDimensions(string imagePath)
    {
        using var stream = File.OpenRead(imagePath);
        using var codec = SKCodec.Create(stream);
        return codec is null
            ? (null, null)
            : (codec.Info.Width, codec.Info.Height);
    }
}
