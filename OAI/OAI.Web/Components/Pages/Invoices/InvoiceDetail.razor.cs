using Microsoft.AspNetCore.Components;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;

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

    protected InvoiceDetailDto? Invoice { get; private set; }

    protected bool IsLoading { get; private set; }

    protected string? ErrorMessage { get; private set; }

    protected override async Task OnParametersSetAsync()
    {
        await LoadInvoiceDetailAsync();
    }

    protected void GoBack()
    {
        NavigationManager.NavigateTo("/invoices");
    }

    protected static string FormatMoney(decimal amount, string currency)
    {
        return $"{amount:N0} {currency}";
    }

    protected static string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Chưa có" : value;
    }

    protected static string GetStatusBadgeClass(string status)
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

    protected static string GetSeverityBadgeClass(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "info" => "text-bg-info",
            "warning" => "text-bg-warning",
            "error" => "text-bg-danger",
            _ => "text-bg-secondary"
        };
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
}