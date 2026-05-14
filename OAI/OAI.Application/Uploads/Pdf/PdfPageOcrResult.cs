namespace OAI.Application.Uploads.Pdf;

public sealed record PdfPageOcrResult(
    bool Succeeded,
    string MergedRawText,
    IReadOnlyList<PdfPageOcrTextResult> Pages,
    decimal? AverageConfidence,
    string? WarningMessage,
    string? ErrorMessage)
{
    public static PdfPageOcrResult Failed(string errorMessage)
        => new(
            Succeeded: false,
            MergedRawText: string.Empty,
            Pages: Array.Empty<PdfPageOcrTextResult>(),
            AverageConfidence: null,
            WarningMessage: null,
            ErrorMessage: errorMessage);
}

public sealed record PdfPageOcrTextResult(
    int PageNumber,
    string RawText,
    decimal? Confidence,
    int CharacterCount);
