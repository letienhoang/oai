namespace OAI.Application.System.Dtos;

public sealed record OcrSettingsDto
{
    public string BasePath { get; init; } = string.Empty;

    public string TessDataPath { get; init; } = string.Empty;

    public string Languages { get; init; } = string.Empty;
}