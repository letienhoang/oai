using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OAI.Infrastructure.DemoData;
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

    private DemoDataSeedResult? SeedResult { get; set; }

    private string? ErrorMessage { get; set; }

    private async Task SeedDemoDataAsync()
    {
        if (!IsDevelopment || IsSeeding)
            return;

        IsSeeding = true;
        ErrorMessage = null;

        try
        {
            SeedResult = await DemoDataSeeder.SeedAsync();
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
}
