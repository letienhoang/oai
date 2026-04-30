using OAI.Application.System.Dtos;

namespace OAI.Application.Abstractions.Services;

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetAsync(
        CancellationToken cancellationToken = default);
}