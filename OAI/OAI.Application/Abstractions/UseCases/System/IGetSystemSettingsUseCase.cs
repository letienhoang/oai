using OAI.Application.System.Dtos;

namespace OAI.Application.Abstractions.UseCases.System;

public interface IGetSystemSettingsUseCase
{
    Task<SystemSettingsDto> ExecuteAsync(
        CancellationToken cancellationToken = default);
}