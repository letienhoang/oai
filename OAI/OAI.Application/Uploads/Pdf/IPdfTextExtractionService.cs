namespace OAI.Application.Uploads.Pdf;

/// <summary>
/// Extracts embedded text from digital PDFs. Scanned PDF rendering and OCR are handled by later upload phases.
/// </summary>
public interface IPdfTextExtractionService
{
    Task<PdfTextExtractionResult> ExtractAsync(
        Stream pdfStream,
        string? fileName,
        CancellationToken cancellationToken = default);
}
