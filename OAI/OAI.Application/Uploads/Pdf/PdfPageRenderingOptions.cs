namespace OAI.Application.Uploads.Pdf;

public sealed class PdfPageRenderingOptions
{
    public string OutputDirectory { get; init; } = string.Empty;

    public string FileNamePrefix { get; init; } = "page";

    public int Dpi { get; init; } = 200;

    public int MaxPages { get; init; } = 20;

    public bool OverwriteExistingFiles { get; init; }
}
