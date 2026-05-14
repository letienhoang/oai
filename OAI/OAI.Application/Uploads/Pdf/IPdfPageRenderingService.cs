namespace OAI.Application.Uploads.Pdf;

public interface IPdfPageRenderingService
{
    Task<PdfPageRenderingResult> RenderAsync(
        Stream pdfStream,
        PdfPageRenderingOptions options,
        CancellationToken cancellationToken = default);
}
