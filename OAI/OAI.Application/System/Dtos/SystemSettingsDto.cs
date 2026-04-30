namespace OAI.Application.System.Dtos;

public sealed record SystemSettingsDto
{
    public FileStorageSettingsDto FileStorage { get; init; } = new();

    public OcrSettingsDto Ocr { get; init; } = new();

    public LlmSettingsDto Llm { get; init; } = new();

    public RuntimeSettingsDto Runtime { get; init; } = new();
}