namespace OAI.Application.Uploads.Pdf;

public interface IPdfPageOcrService
{
    Task<PdfPageOcrResult> OcrAsync(
        IReadOnlyList<PdfStoredPagePreviewResult> previews,
        CancellationToken cancellationToken = default);
}
