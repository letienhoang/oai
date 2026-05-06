namespace OAI.Infrastructure.Options;

public sealed class LlmOptions
{
    public bool Enabled { get; set; } = false;

    public string Provider { get; set; } = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4.1-mini";

    public int MaxInputCharacters { get; set; } = 12000;
}