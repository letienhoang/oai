using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.System;
using OAI.Application.System.Dtos;
using OAI.Web.Localization;

namespace OAI.Web.Components.Pages.Settings;

public partial class SettingsPage
{
    [Inject]
    private IGetSystemSettingsUseCase GetSystemSettingsUseCase { get; set; } = default!;

    [Inject]
    private ILogger<SettingsPage> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    private SystemSettingsDto? Settings { get; set; }

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadSettingsAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Logger.LogInformation("Loading system settings.");

            Settings = await GetSystemSettingsUseCase.ExecuteAsync();

            Logger.LogInformation("System settings loaded successfully.");
        }
        catch (Exception ex)
        {
            Settings = null;
            ErrorMessage = L["SystemSettingsLoadFailed"];

            Logger.LogError(ex, "Failed to load system settings.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string GetEnvironmentBadgeClass(string environmentName)
    {
        return environmentName.ToLowerInvariant() switch
        {
            "development" => "text-bg-warning",
            "staging" => "text-bg-info",
            "production" => "text-bg-success",
            _ => "text-bg-secondary"
        };
    }
}
