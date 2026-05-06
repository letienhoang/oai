using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Abstractions.UseCases.Vendors;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Vendors.Dtos;
using OAI.Domain.Enums;
using OAI.Web.Localization;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceList
{
    private const int DefaultPageSize = 10;

    [Inject]
    private IGetInvoiceListUseCase GetInvoiceListUseCase { get; set; } = default!;

    [Inject]
    private IGetVendorOptionsUseCase GetVendorOptionsUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceList> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    private List<InvoiceListItemDto> Invoices { get; set; } = new();

    private List<VendorOptionDto> VendorOptions { get; set; } = new();

    private string? Keyword { get; set; }

    private InvoiceListFilterDto Filter { get; set; } = new();

    private string? SelectedStatus { get; set; }

    private string? SelectedVendorId { get; set; }

    private DateOnly? IssueDateFrom { get; set; }

    private DateOnly? IssueDateTo { get; set; }

    private int PageNumber { get; set; } = 1;

    private int PageSize { get; set; } = DefaultPageSize;

    private int TotalItems { get; set; }

    private int TotalPages { get; set; }

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    private bool CanGoPrevious => PageNumber > 1;

    private bool CanGoNext => PageNumber < TotalPages;

    private static IReadOnlyList<InvoiceStatus> StatusOptions { get; } =
    [
        InvoiceStatus.Draft,
        InvoiceStatus.PendingReview,
        InvoiceStatus.Approved,
        InvoiceStatus.Rejected,
        InvoiceStatus.Exported
    ];

    protected override async Task OnInitializedAsync()
    {
        await LoadVendorOptionsAsync();
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
        SelectedStatus = null;
        SelectedVendorId = null;
        IssueDateFrom = null;
        IssueDateTo = null;
        Filter = new InvoiceListFilterDto();
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

    private async Task LoadVendorOptionsAsync()
    {
        try
        {
            var vendors = await GetVendorOptionsUseCase.ExecuteAsync();
            VendorOptions = vendors.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load vendor filter options.");
            VendorOptions.Clear();
        }
    }

    private InvoiceListFilterDto BuildFilter()
    {
        InvoiceStatus? status = null;

        if (!string.IsNullOrWhiteSpace(SelectedStatus) &&
            Enum.TryParse<InvoiceStatus>(SelectedStatus, ignoreCase: true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        Guid? vendorId = null;

        if (!string.IsNullOrWhiteSpace(SelectedVendorId) &&
            Guid.TryParse(SelectedVendorId, out var parsedVendorId))
        {
            vendorId = parsedVendorId;
        }

        return Filter with
        {
            Keyword = Keyword,
            Status = status,
            VendorId = vendorId,
            IssueDateFrom = IssueDateFrom,
            IssueDateTo = IssueDateTo
        };
    }

    private string GetStatusLabel(InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Draft => L["Draft"],
            InvoiceStatus.PendingReview => L["PendingReview"],
            InvoiceStatus.Approved => L["Approved"],
            InvoiceStatus.Rejected => L["Rejected"],
            InvoiceStatus.Exported => L["Exported"],
            _ => status.ToString()
        };
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

            var effectiveFilter = BuildFilter();

            var result = await GetInvoiceListUseCase.ExecuteAsync(
                new GetInvoiceListRequestDto
                {
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    Filter = effectiveFilter
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
            ErrorMessage = L["InvoiceListLoadFailed"];
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
