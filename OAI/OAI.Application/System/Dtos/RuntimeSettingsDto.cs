namespace OAI.Application.System.Dtos;

public sealed record RuntimeSettingsDto
{
    public string EnvironmentName { get; init; } = string.Empty;

    public string ApplicationName { get; init; } = string.Empty;

    public string ContentRootPath { get; init; } = string.Empty;
}