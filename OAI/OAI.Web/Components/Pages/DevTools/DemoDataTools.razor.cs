using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OAI.Infrastructure.DemoData;
using OAI.Web.Components.Shared;
using OAI.Web.Localization;

namespace OAI.Web.Components.Pages.DevTools;

public partial class DemoDataTools
{
    [Inject]
    private DemoDataSeeder DemoDataSeeder { get; set; } = default!;

    [Inject]
    private IWebHostEnvironment Environment { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [Inject]
    private ILogger<DemoDataTools> Logger { get; set; } = default!;

    private bool IsDevelopment => Environment.IsDevelopment();

    private bool IsSeeding { get; set; }

    private bool IsResetting { get; set; }

    private DemoDataSeedResult? SeedResult { get; set; }

    private DemoDataResetResult? ResetResult { get; set; }

    private ConfirmDialog? ConfirmDialog { get; set; }

    private string? ErrorMessage { get; set; }

    private async Task ConfirmResetDemoData()
    {
        if (ConfirmDialog is null)
            return;

        var confirmed = await ConfirmDialog.ShowAsync(
            title: L["ConfirmResetDemoDataTitle"],
            message: L["ConfirmResetDemoDataMessage"],
            confirmText: L["ResetDemoData"],
            cancelText: L["Cancel"],
            confirmButtonClass: "btn btn-danger");

        if (confirmed)
        {
            await ResetDemoDataAsync();
        }
    }

    private async Task SeedDemoDataAsync()
    {
        if (!IsDevelopment || IsSeeding || IsResetting)
            return;

        IsSeeding = true;
        ErrorMessage = null;

        try
        {
            SeedResult = await DemoDataSeeder.SeedAsync();
            ResetResult = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to seed demo data from dev tools page.");
            ErrorMessage = L["DemoDataSeedFailed"];
        }
        finally
        {
            IsSeeding = false;
        }
    }

    private async Task ResetDemoDataAsync()
    {
        if (!IsDevelopment || IsResetting || IsSeeding)
            return;

        IsResetting = true;
        ErrorMessage = null;

        try
        {
            ResetResult = await DemoDataSeeder.ResetAsync();
            SeedResult = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to reset demo data from dev tools page.");
            ErrorMessage = L["DemoDataResetFailed"];
        }
        finally
        {
            IsResetting = false;
        }
    }
}
