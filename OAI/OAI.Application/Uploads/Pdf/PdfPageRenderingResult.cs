namespace OAI.Application.Uploads.Pdf;

public sealed record PdfPageRenderingResult(
    bool Succeeded,
    int PageCount,
    IReadOnlyList<PdfRenderedPageResult> Pages,
    string? WarningMessage,
    string? ErrorMessage)
{
    public static PdfPageRenderingResult Failed(string errorMessage)
        => new(
            Succeeded: false,
            PageCount: 0,
            Pages: Array.Empty<PdfRenderedPageResult>(),
            WarningMessage: null,
            ErrorMessage: errorMessage);
}

public sealed record PdfRenderedPageResult(
    int PageNumber,
    string ImagePath,
    string ContentType,
    long FileSizeBytes,
    int? Width,
    int? Height);
