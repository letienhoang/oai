using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Services;
using OAI.Application.Abstractions.UseCases.System;
using OAI.Application.System.Dtos;

namespace OAI.Application.UseCases.System;

public sealed class GetSystemSettingsUseCase : IGetSystemSettingsUseCase
{
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly ILogger<GetSystemSettingsUseCase> _logger;

    public GetSystemSettingsUseCase(
        ISystemSettingsService systemSettingsService,
        ILogger<GetSystemSettingsUseCase> logger)
    {
        _systemSettingsService = systemSettingsService;
        _logger = logger;
    }

    public async Task<SystemSettingsDto> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting system settings.");

        return await _systemSettingsService.GetAsync(cancellationToken);
    }
}