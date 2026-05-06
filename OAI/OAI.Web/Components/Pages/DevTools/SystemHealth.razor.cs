using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OAI.Infrastructure.SystemHealth;
using OAI.Web.Localization;

namespace OAI.Web.Components.Pages.DevTools;

public partial class SystemHealth
{
    [Inject]
    private SystemHealthService SystemHealthService { get; set; } = default!;

    [Inject]
    private IWebHostEnvironment Environment { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [Inject]
    private ILogger<SystemHealth> Logger { get; set; } = default!;

    private bool IsDevelopment => Environment.IsDevelopment();

    private SystemHealthCheckResult? Health { get; set; }

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!IsDevelopment)
            return;

        await LoadAsync();
    }

    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!IsDevelopment)
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Health = await SystemHealthService.CheckAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to check system health.");
            ErrorMessage = L["SystemHealthCheckFailed"];
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string FormatDateTime(DateTimeOffset value)
    {
        return value.ToLocalTime().ToString("g");
    }

    private RenderFragment StatusBadge(bool isOk) => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", isOk ? "badge text-bg-success" : "badge text-bg-danger");
        builder.AddContent(2, isOk ? L["Ok"] : L["Failed"]);
        builder.CloseElement();
    };

    private RenderFragment HealthBadge(bool isHealthy) => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", isHealthy ? "badge text-bg-success" : "badge text-bg-warning");
        builder.AddContent(2, isHealthy ? L["Healthy"] : L["NeedsAttention"]);
        builder.CloseElement();
    };

    private RenderFragment Metric(string label, int value) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "col-6");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "border rounded-3 p-3 h-100");

        builder.OpenElement(4, "div");
        builder.AddAttribute(5, "class", "text-secondary fw-semibold small");
        builder.AddContent(6, label);
        builder.CloseElement();

        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "fs-3 fw-bold mt-1");
        builder.AddContent(9, value);
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();
    };
}
