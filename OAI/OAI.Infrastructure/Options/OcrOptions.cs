namespace OAI.Infrastructure.Options;

public sealed class OcrOptions
{
    public string TessDataPath { get; set; } = "tessdata";
    public string Languages { get; set; } = "eng";
    public string? BasePath { get; set; }
}