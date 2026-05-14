namespace OAI.Application.Uploads.Pdf;

public interface IPdfPagePreviewStorageService
{
    Task<PdfPagePreviewStorageResult> StoreAsync(
        Guid uploadBatchFileId,
        IReadOnlyList<PdfRenderedPageResult> renderedPages,
        CancellationToken cancellationToken = default);

    Task LinkPreviewsToInvoiceAsync(
        Guid uploadBatchFileId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
