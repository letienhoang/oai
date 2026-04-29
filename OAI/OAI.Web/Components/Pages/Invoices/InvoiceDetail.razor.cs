using Microsoft.AspNetCore.Components;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceDetail
{
    [Parameter]
    public Guid InvoiceId { get; set; }

    [Inject]
    private IGetInvoiceDetailUseCase GetInvoiceDetailUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceDetail> Logger { get; set; } = default!;
    
    [Inject]
    private UserTimeZoneService UserTimeZoneService { get; set; } = default!;
    
    [Inject]
    private IApproveInvoiceUseCase ApproveInvoiceUseCase { get; set; } = default!;

    private InvoiceDetailDto? Invoice { get; set; }

    private TimeZoneInfo UserTimeZone { get; set; } = TimeZoneInfo.Utc;

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }
    
    private bool IsApproving { get; set; }

    private string? SuccessMessage { get; set; }

    private string? ActionErrorMessage { get; set; }

    private bool CanApprove =>
        Invoice is not null &&
        string.Equals(Invoice.Status, "PendingReview", StringComparison.OrdinalIgnoreCase) &&
        !Invoice.ValidationIssues.Any(x =>
            string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase) &&
            !x.IsResolved);

    protected override async Task OnParametersSetAsync()
    {
        await LoadInvoiceDetailAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        UserTimeZone = await UserTimeZoneService.GetUserTimeZoneAsync();
        StateHasChanged();
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/invoices");
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        return $"{amount:N0} {currency}";
    }

    private static string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Chưa có" : value;
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

    private void GoToEdit()
    {
        NavigationManager.NavigateTo($"/invoices/{InvoiceId}/edit");
    }
    
    private void GoToCompare()
    {
        NavigationManager.NavigateTo($"/invoices/{InvoiceId}/compare");
    }

    private async Task LoadInvoiceDetailAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            Logger.LogInformation("Loading invoice detail. InvoiceId: {InvoiceId}", InvoiceId);

            Invoice = await GetInvoiceDetailUseCase.ExecuteAsync(
                new GetInvoiceDetailRequestDto
                {
                    InvoiceId = InvoiceId
                });

            Logger.LogInformation(
                "Invoice detail loaded. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
                Invoice.InvoiceId,
                Invoice.InvoiceNumber);
        }
        catch (Exception ex)
        {
            Invoice = null;
            ErrorMessage = "Không thể tải chi tiết hóa đơn. Vui lòng kiểm tra log để biết thêm chi tiết.";

            Logger.LogError(ex, "Failed to load invoice detail. InvoiceId: {InvoiceId}", InvoiceId);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private async Task ApproveAsync()
    {
        if (Invoice is null)
            return;

        SuccessMessage = null;
        ActionErrorMessage = null;
        IsApproving = true;

        try
        {
            Logger.LogInformation(
                "Approving invoice from detail page. InvoiceId: {InvoiceId}",
                Invoice.InvoiceId);

            var result = await ApproveInvoiceUseCase.ExecuteAsync(
                new ApproveInvoiceRequestDto
                {
                    InvoiceId = Invoice.InvoiceId
                });

            SuccessMessage = "Hóa đơn đã được xác nhận thành công.";

            Logger.LogInformation(
                "Invoice approved from detail page. InvoiceId: {InvoiceId}, Status: {Status}",
                result.InvoiceId,
                result.Status);

            await LoadInvoiceDetailAsync();
        }
        catch (Exception ex)
        {
            ActionErrorMessage = "Không thể xác nhận hóa đơn. Vui lòng kiểm tra lỗi validation hoặc log hệ thống.";

            Logger.LogError(
                ex,
                "Failed to approve invoice from detail page. InvoiceId: {InvoiceId}",
                Invoice.InvoiceId);
        }
        finally
        {
            IsApproving = false;
        }
    }
}
