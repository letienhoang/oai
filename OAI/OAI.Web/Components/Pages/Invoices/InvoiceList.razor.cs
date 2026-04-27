using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceList
{
    private const int DefaultPageSize = 10;

    [Inject]
    private IGetInvoiceListUseCase GetInvoiceListUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceList> Logger { get; set; } = default!;

    private List<InvoiceListItemDto> Invoices { get; set; } = new();

    private string? Keyword { get; set; }

    private int PageNumber { get; set; } = 1;

    private int PageSize { get; set; } = DefaultPageSize;

    private int TotalItems { get; set; }

    private int TotalPages { get; set; }

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    private bool CanGoPrevious => PageNumber > 1;

    private bool CanGoNext => PageNumber < TotalPages;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoicesAsync();
    }

    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadInvoicesAsync();
    }

    private async Task ClearSearchAsync()
    {
        Keyword = null;
        PageNumber = 1;
        await LoadInvoicesAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        PageNumber--;
        await LoadInvoicesAsync();
    }

    private async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        PageNumber++;
        await LoadInvoicesAsync();
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    private void GoToUpload()
    {
        NavigationManager.NavigateTo("/invoices/upload");
    }

    private void GoToDetail(Guid invoiceId)
    {
        NavigationManager.NavigateTo($"/invoices/{invoiceId}");
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

    private static string FormatMoney(decimal amount, string currency)
    {
        return $"{amount:N0} {currency}";
    }

    private async Task LoadInvoicesAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            Logger.LogInformation(
                "Loading invoice list. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}",
                PageNumber,
                PageSize,
                Keyword);

            var result = await GetInvoiceListUseCase.ExecuteAsync(
                new GetInvoiceListRequestDto
                {
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    Keyword = Keyword
                });

            Invoices = result.Items.ToList();
            TotalItems = result.TotalItems;
            TotalPages = result.TotalPages;

            Logger.LogInformation(
                "Invoice list loaded. TotalItems: {TotalItems}, CurrentPageItems: {CurrentPageItems}",
                TotalItems,
                Invoices.Count);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không thể tải danh sách hóa đơn. Vui lòng kiểm tra log để biết thêm chi tiết.";
            Invoices.Clear();
            TotalItems = 0;
            TotalPages = 0;

            Logger.LogError(ex, "Failed to load invoice list.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}