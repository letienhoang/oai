namespace OAI.Application.Uploads.Pdf;

public sealed record PdfTextExtractionResult(
    bool Succeeded,
    bool HasUsableText,
    int PageCount,
    string FullText,
    IReadOnlyList<PdfPageTextExtractionResult> Pages,
    string? WarningMessage,
    string? ErrorMessage)
{
    public static PdfTextExtractionResult Failed(string errorMessage)
        => new(
            Succeeded: false,
            HasUsableText: false,
            PageCount: 0,
            FullText: string.Empty,
            Pages: Array.Empty<PdfPageTextExtractionResult>(),
            WarningMessage: null,
            ErrorMessage: errorMessage);
}

public sealed record PdfPageTextExtractionResult(
    int PageNumber,
    string Text,
    int CharacterCount);
