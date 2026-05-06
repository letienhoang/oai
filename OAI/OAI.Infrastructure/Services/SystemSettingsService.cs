using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OAI.Application.Abstractions.Services;
using OAI.Application.System.Dtos;
using OAI.Infrastructure.Options;

namespace OAI.Infrastructure.Services;

public sealed class SystemSettingsService : ISystemSettingsService
{
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly OcrOptions _ocrOptions;
    private readonly LlmOptions _llmOptions;
    private readonly IHostEnvironment _hostEnvironment;

    public SystemSettingsService(
        IOptions<FileStorageOptions> fileStorageOptions,
        IOptions<OcrOptions> ocrOptions,
        IOptions<LlmOptions> llmOptions,
        IHostEnvironment hostEnvironment)
    {
        _fileStorageOptions = fileStorageOptions.Value;
        _ocrOptions = ocrOptions.Value;
        _llmOptions = llmOptions.Value;
        _hostEnvironment = hostEnvironment;
    }

    public Task<SystemSettingsDto> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var dto = new SystemSettingsDto
        {
            FileStorage = new FileStorageSettingsDto
            {
                BasePath = DisplayEmpty(_fileStorageOptions.BasePath),
                RootPath = DisplayEmpty(_fileStorageOptions.RootPath),
                InvoiceFolder = DisplayEmpty(_fileStorageOptions.InvoiceFolder),
                MaxFileSizeBytes = _fileStorageOptions.MaxFileSizeBytes,
                MaxFileSizeDisplay = FormatFileSize(_fileStorageOptions.MaxFileSizeBytes)
            },
            Ocr = new OcrSettingsDto
            {
                BasePath = DisplayEmpty(_ocrOptions.BasePath),
                TessDataPath = DisplayEmpty(_ocrOptions.TessDataPath),
                Languages = DisplayEmpty(_ocrOptions.Languages)
            },
            Llm = new LlmSettingsDto
            {
                Enabled = _llmOptions.Enabled,
                Provider = DisplayEmpty(_llmOptions.Provider),
                Model = DisplayEmpty(_llmOptions.Model),
                MaxInputCharacters = _llmOptions.MaxInputCharacters,
                HasApiKey = !string.IsNullOrWhiteSpace(_llmOptions.ApiKey),
                ApiKeyStatus = string.IsNullOrWhiteSpace(_llmOptions.ApiKey)
                    ? "Not configured"
                    : "Configured"
            },
            Runtime = new RuntimeSettingsDto
            {
                EnvironmentName = _hostEnvironment.EnvironmentName,
                ApplicationName = _hostEnvironment.ApplicationName,
                ContentRootPath = _hostEnvironment.ContentRootPath
            }
        };

        return Task.FromResult(dto);
    }

    private static string DisplayEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(empty)" : value;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024d:N1} KB";

        return $"{bytes / 1024d / 1024d:N1} MB";
    }
}