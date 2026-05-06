namespace OAI.Application.System.Dtos;

public sealed record FileStorageSettingsDto
{
    public string BasePath { get; init; } = string.Empty;

    public string RootPath { get; init; } = string.Empty;

    public string InvoiceFolder { get; init; } = string.Empty;

    public long MaxFileSizeBytes { get; init; }

    public string MaxFileSizeDisplay { get; init; } = string.Empty;
}