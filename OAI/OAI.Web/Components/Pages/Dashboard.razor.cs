using Microsoft.AspNetCore.Components;
using OAI.Application.Abstractions.UseCases.Dashboard;
using OAI.Application.Dashboard.Dtos;

namespace OAI.Web.Components.Pages;

public partial class Dashboard
{
    [Inject]
    private IGetDashboardSummaryUseCase GetDashboardSummaryUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<Dashboard> Logger { get; set; } = default!;

    private DashboardSummaryDto? Summary { get; set; }

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardAsync();
    }

    private async Task ReloadAsync()
    {
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

            Summary = await GetDashboardSummaryUseCase.ExecuteAsync();

            Logger.LogInformation(
                "Dashboard summary loaded. TotalInvoices: {TotalInvoices}, TotalValidationIssues: {TotalValidationIssues}",
                Summary.TotalInvoices,
                Summary.TotalValidationIssues);
        }
        catch (Exception ex)
        {
            Summary = null;
            ErrorMessage = "Không thể tải dữ liệu dashboard. Vui lòng kiểm tra log để biết thêm chi tiết.";

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

    private static string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Chưa có" : value;
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