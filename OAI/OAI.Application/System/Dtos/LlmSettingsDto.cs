namespace OAI.Application.System.Dtos;

public sealed record LlmSettingsDto
{
    public bool Enabled { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int MaxInputCharacters { get; init; }

    public bool HasApiKey { get; init; }

    public string ApiKeyStatus { get; init; } = string.Empty;
}