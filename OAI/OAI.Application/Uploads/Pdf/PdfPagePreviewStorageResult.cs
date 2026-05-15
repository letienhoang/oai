namespace OAI.Application.Uploads.Pdf;

public sealed record PdfPagePreviewStorageResult(
    bool Succeeded,
    IReadOnlyList<PdfStoredPagePreviewResult> Previews,
    string? ErrorMessage)
{
    public static PdfPagePreviewStorageResult Failed(string errorMessage)
        => new(
            Succeeded: false,
            Previews: Array.Empty<PdfStoredPagePreviewResult>(),
            ErrorMessage: errorMessage);
}

public sealed record PdfStoredPagePreviewResult(
    int PageNumber,
    string PreviewFilePath,
    string ContentType,
    long FileSizeBytes);
