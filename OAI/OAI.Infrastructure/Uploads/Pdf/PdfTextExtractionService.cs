using Microsoft.Extensions.Logging;
using OAI.Application.Uploads.Pdf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace OAI.Infrastructure.Uploads.Pdf;

public sealed class PdfTextExtractionService : IPdfTextExtractionService
{
    private const int MinimumUsableTextLength = 50;
    private const string ScannedPdfRequiredMessage =
        "The PDF does not contain enough embedded text. Scanned PDF OCR processing is required.";

    private readonly ILogger<PdfTextExtractionService> _logger;

    public PdfTextExtractionService(ILogger<PdfTextExtractionService> logger)
    {
        _logger = logger;
    }

    public async Task<PdfTextExtractionResult> ExtractAsync(
        Stream pdfStream,
        string? fileName,
        CancellationToken cancellationToken = default)
    {
        if (pdfStream is null || !pdfStream.CanRead)
        {
            return PdfTextExtractionResult.Failed("PDF text extraction failed because the PDF stream is invalid.");
        }

        var originalPosition = pdfStream.CanSeek ? pdfStream.Position : (long?)null;

        try
        {
            var extractionStream = await PrepareSeekableStreamAsync(
                pdfStream,
                cancellationToken);
            await using var copiedStream = ReferenceEquals(extractionStream, pdfStream)
                ? null
                : extractionStream;

            using var document = PdfDocument.Open(extractionStream);

            var pageResults = new List<PdfPageTextExtractionResult>();
            var pageSections = new List<string>();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageText = NormalizeText(ExtractPageText(page));
                pageResults.Add(new PdfPageTextExtractionResult(
                    page.Number,
                    pageText,
                    pageText.Length));

                pageSections.Add($"--- Page {page.Number} ---\n{pageText}");
            }

            var fullText = string.Join("\n\n", pageSections).Trim();
            var combinedPageTextLength = string.Join("\n", pageResults.Select(page => page.Text)).Trim().Length;
            var hasUsableText = combinedPageTextLength >= MinimumUsableTextLength;

            return new PdfTextExtractionResult(
                Succeeded: true,
                HasUsableText: hasUsableText,
                PageCount: document.NumberOfPages,
                FullText: fullText,
                Pages: pageResults,
                WarningMessage: hasUsableText ? null : ScannedPdfRequiredMessage,
                ErrorMessage: null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PdfDocumentFormatException ex)
        {
            _logger.LogWarning(
                ex,
                "PDF text extraction failed because the PDF could not be parsed. FileName: {FileName}",
                fileName);

            return PdfTextExtractionResult.Failed("PDF text extraction failed because the PDF file is invalid or unreadable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "PDF text extraction failed unexpectedly. FileName: {FileName}",
                fileName);

            return PdfTextExtractionResult.Failed("PDF text extraction failed unexpectedly.");
        }
        finally
        {
            if (originalPosition.HasValue && pdfStream.CanSeek)
            {
                pdfStream.Position = originalPosition.Value;
            }
        }
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

    private static string ExtractPageText(UglyToad.PdfPig.Content.Page page)
    {
        var pageText = ContentOrderTextExtractor.GetText(page);
        return string.IsNullOrWhiteSpace(pageText) ? page.Text : pageText;
    }

    private static string NormalizeText(string? text)
        => (text ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Trim();
}
