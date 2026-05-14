using Microsoft.Extensions.Logging.Abstractions;
using OAI.Infrastructure.Uploads.Pdf;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace OAI.Infrastructure.Tests.Uploads.Pdf;

public sealed class PdfTextExtractionServiceTests
{
    private readonly PdfTextExtractionService _service = new(
        NullLogger<PdfTextExtractionService>.Instance);

    [Fact]
    public async Task ExtractAsync_ExtractsTextFromTextBasedPdf()
    {
        var pdfBytes = CreatePdf(
            "Invoice INV-1001 from Acme Supplies. Total amount 150000 VND. Payment due soon.",
            "Second page contains tax code, address, and line item details for validation.");

        await using var stream = new MemoryStream(pdfBytes);

        var result = await _service.ExtractAsync(stream, "invoice.pdf");

        Assert.True(result.Succeeded);
        Assert.True(result.HasUsableText);
        Assert.Contains("--- Page 1 ---", result.FullText);
        Assert.Contains("Invoice INV-1001", result.FullText);
        Assert.Contains("--- Page 2 ---", result.FullText);
    }

    [Fact]
    public async Task ExtractAsync_ReturnsHasUsableTextTrueWhenExtractedTextIsLongEnough()
    {
        var pdfBytes = CreatePdf(
            "This digital invoice PDF contains enough embedded text to bypass OCR and feed parsing.");

        await using var stream = new MemoryStream(pdfBytes);

        var result = await _service.ExtractAsync(stream, "invoice.pdf");

        Assert.True(result.Succeeded);
        Assert.True(result.HasUsableText);
        Assert.Null(result.WarningMessage);
    }

    [Fact]
    public async Task ExtractAsync_ReturnsHasUsableTextFalseWhenTextIsTooShort()
    {
        var pdfBytes = CreatePdf("Tiny");

        await using var stream = new MemoryStream(pdfBytes);

        var result = await _service.ExtractAsync(stream, "invoice.pdf");

        Assert.True(result.Succeeded);
        Assert.False(result.HasUsableText);
        Assert.Equal(
            "The PDF does not contain enough embedded text. Scanned PDF OCR processing is required.",
            result.WarningMessage);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractAsync_ReturnsFailedResultForInvalidPdfBytes()
    {
        await using var stream = new MemoryStream([0x01, 0x02, 0x03, 0x04]);

        var result = await _service.ExtractAsync(stream, "broken.pdf");

        Assert.False(result.Succeeded);
        Assert.False(result.HasUsableText);
        Assert.Equal(0, result.PageCount);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractAsync_ExtractsPageCountAndPerPageResults()
    {
        var pdfBytes = CreatePdf(
            "Page one has embedded invoice text with enough detail for extraction.",
            "Page two has additional embedded invoice text and totals.");

        await using var stream = new MemoryStream(pdfBytes);

        var result = await _service.ExtractAsync(stream, "invoice.pdf");

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.PageCount);
        Assert.Collection(
            result.Pages,
            page =>
            {
                Assert.Equal(1, page.PageNumber);
                Assert.Contains("Page one", page.Text);
                Assert.Equal(page.Text.Length, page.CharacterCount);
            },
            page =>
            {
                Assert.Equal(2, page.PageNumber);
                Assert.Contains("Page two", page.Text);
                Assert.Equal(page.Text.Length, page.CharacterCount);
            });
    }

    [Fact]
    public async Task ExtractAsync_RestoresSeekableStreamPosition()
    {
        var pdfBytes = CreatePdf(
            "This text is long enough for the extraction result to be considered usable.");

        await using var stream = new MemoryStream(pdfBytes);
        stream.Position = 5;

        var result = await _service.ExtractAsync(stream, "invoice.pdf");

        Assert.True(result.Succeeded);
        Assert.Equal(5, stream.Position);
    }

    private static byte[] CreatePdf(params string[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var pageText in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            page.AddText(pageText, 12, new PdfPoint(25, 780), font);
        }

        return builder.Build();
    }
}
