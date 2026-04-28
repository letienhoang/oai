using Microsoft.AspNetCore.Components;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos.ExtractionComparison;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceExtractionCompare
{
    [Parameter]
    public Guid InvoiceId { get; set; }

    [Inject]
    private ICompareInvoiceExtractionUseCase CompareInvoiceExtractionUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceExtractionCompare> Logger { get; set; } = default!;

    private InvoiceExtractionComparisonDto? Comparison { get; set; }

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await LoadComparisonAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadComparisonAsync();
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo($"/invoices/{InvoiceId}");
    }

    private async Task LoadComparisonAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Logger.LogInformation(
                "Loading extraction comparison. InvoiceId: {InvoiceId}",
                InvoiceId);

            Comparison = await CompareInvoiceExtractionUseCase.ExecuteAsync(
                new CompareInvoiceExtractionRequestDto
                {
                    InvoiceId = InvoiceId
                });

            Logger.LogInformation(
                "Extraction comparison loaded. InvoiceId: {InvoiceId}",
                InvoiceId);
        }
        catch (Exception ex)
        {
            Comparison = null;
            ErrorMessage = "Không thể so sánh kết quả trích xuất. Vui lòng kiểm tra log để biết thêm chi tiết.";

            Logger.LogError(
                ex,
                "Failed to load extraction comparison. InvoiceId: {InvoiceId}",
                InvoiceId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Chưa có" : value;
    }

    private static string FormatDate(DateOnly? value)
    {
        return value?.ToString("dd/MM/yyyy") ?? "Chưa có";
    }

    private static string FormatMoney(decimal? amount, string currency)
    {
        if (amount is null)
            return "Chưa có";

        if (string.IsNullOrWhiteSpace(currency))
            currency = "VND";

        return $"{amount.Value:N0} {currency}";
    }
}