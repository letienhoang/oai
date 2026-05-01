using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos.ExtractionComparison;
using OAI.Web.Localization;

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

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

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
            ErrorMessage = L["ExtractionComparisonLoadFailed"];

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

    private string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? L["NotAvailable"] : value;
    }

    private string FormatDate(DateOnly? value)
    {
        return value?.ToString("dd/MM/yyyy") ?? L["NotAvailable"];
    }

    private string FormatMoney(decimal? amount, string currency)
    {
        if (amount is null)
            return L["NotAvailable"];

        if (string.IsNullOrWhiteSpace(currency))
            currency = "VND";

        return $"{amount.Value:N0} {currency}";
    }
}
