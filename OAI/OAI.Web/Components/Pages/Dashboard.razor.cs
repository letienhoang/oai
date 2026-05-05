using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Dashboard;
using OAI.Application.Dashboard.Dtos;
using OAI.Web.Localization;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages;

public partial class Dashboard
{
    [Inject]
    private IGetDashboardSummaryUseCase GetDashboardSummaryUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<Dashboard> Logger { get; set; } = default!;

    [Inject]
    private UserTimeZoneService UserTimeZoneService { get; set; } = default!;
    
    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [Inject]
    private LocalizedMessageResolver LocalizedMessageResolver { get; set; } = default!;

    private DashboardSummaryDto? Summary { get; set; }

    private DashboardFilterDto Filter { get; set; } = new();

    private DateOnly? IssueDateFrom { get; set; }

    private DateOnly? IssueDateTo { get; set; }

    private TimeZoneInfo UserTimeZone { get; set; } = TimeZoneInfo.Utc;

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    private string? FilterErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        UserTimeZone = await UserTimeZoneService.GetUserTimeZoneAsync();
        StateHasChanged();
    }

    private async Task ReloadAsync()
    {
        await LoadDashboardAsync();
    }

    private DashboardFilterDto BuildFilter()
    {
        return Filter with
        {
            IssueDateFrom = IssueDateFrom,
            IssueDateTo = IssueDateTo
        };
    }

    private async Task ApplyFilterAsync()
    {
        FilterErrorMessage = null;

        if (IssueDateFrom.HasValue &&
            IssueDateTo.HasValue &&
            IssueDateFrom.Value > IssueDateTo.Value)
        {
            FilterErrorMessage = L["InvalidDateRange"];
            return;
        }

        await LoadDashboardAsync();
    }

    private async Task ClearFilterAsync()
    {
        FilterErrorMessage = null;
        IssueDateFrom = null;
        IssueDateTo = null;
        Filter = new DashboardFilterDto();

        await LoadDashboardAsync();
    }

    private void GoToUpload()
    {
        NavigationManager.NavigateTo("/invoices/upload");
    }

    private void GoToInvoiceList()
    {
        NavigationManager.NavigateTo("/invoices");
    }

    private void GoToValidationIssues()
    {
        NavigationManager.NavigateTo("/invoices/validation");
    }

    private void GoToInvoiceDetail(Guid invoiceId)
    {
        NavigationManager.NavigateTo($"/invoices/{invoiceId}");
    }

    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Logger.LogInformation("Loading dashboard summary.");

            Summary = await GetDashboardSummaryUseCase.ExecuteAsync(
                new GetDashboardSummaryRequestDto
                {
                    Filter = BuildFilter()
                });

            Logger.LogInformation(
                "Dashboard summary loaded. TotalInvoices: {TotalInvoices}, TotalValidationIssues: {TotalValidationIssues}",
                Summary.TotalInvoices,
                Summary.TotalValidationIssues);
        }
        catch (Exception ex)
        {
            Summary = null;
            ErrorMessage = L["DashboardLoadFailed"];

            Logger.LogError(ex, "Failed to load dashboard summary.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private int GetPercent(int value)
    {
        if (Summary is null || Summary.TotalInvoices <= 0)
            return 0;

        return (int)Math.Round(value * 100.0 / Summary.TotalInvoices);
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        return $"{amount:N0} {currency}";
    }

    private string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? L["NotAvailable"] : value;
    }

    private string LocalizeMessage(
        string? messageCode,
        IReadOnlyDictionary<string, string>? parameters,
        string? fallbackMessage)
    {
        return LocalizedMessageResolver.Resolve(messageCode, parameters, fallbackMessage);
    }

    private string FormatDateTime(DateTimeOffset value)
    {
        var localTime = TimeZoneInfo.ConvertTime(value, UserTimeZone);
        return localTime.ToString("dd/MM/yyyy HH:mm");
    }

    private static string GetStatusBadgeClass(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "draft" => "text-bg-secondary",
            "pendingreview" => "text-bg-warning",
            "approved" => "text-bg-success",
            "rejected" => "text-bg-danger",
            "exported" => "text-bg-info",
            _ => "text-bg-secondary"
        };
    }

    private static string GetSeverityBadgeClass(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "info" => "text-bg-info",
            "warning" => "text-bg-warning",
            "error" => "text-bg-danger",
            _ => "text-bg-secondary"
        };
    }
}
