using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Web.Localization;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceValidation
{
    private const int DefaultPageSize = 10;

    [Inject]
    private IGetValidationIssueListUseCase GetValidationIssueListUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceValidation> Logger { get; set; } = default!;

    [Inject]
    private UserTimeZoneService UserTimeZoneService { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    private List<ValidationIssueListItemDto> Issues { get; set; } = new();

    private TimeZoneInfo UserTimeZone { get; set; } = TimeZoneInfo.Utc;

    private string? Keyword { get; set; }

    private string? Severity { get; set; }

    private string? ResolvedFilter { get; set; }

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
        await LoadIssuesAsync();
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
        await LoadIssuesAsync();
    }

    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadIssuesAsync();
    }

    private async Task ClearSearchAsync()
    {
        Keyword = null;
        Severity = null;
        ResolvedFilter = null;
        PageNumber = 1;
        await LoadIssuesAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        PageNumber--;
        await LoadIssuesAsync();
    }

    private async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        PageNumber++;
        await LoadIssuesAsync();
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    private void GoToInvoiceDetail(Guid invoiceId)
    {
        NavigationManager.NavigateTo($"/invoices/{invoiceId}");
    }

    private string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? L["NotAvailable"] : value;
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

    private async Task LoadIssuesAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var isResolved = ResolvedFilter?.ToLowerInvariant() switch
            {
                "open" => false,
                "resolved" => true,
                _ => (bool?)null
            };

            Logger.LogInformation(
                "Loading validation issues. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}, Severity: {Severity}, IsResolved: {IsResolved}",
                PageNumber,
                PageSize,
                Keyword,
                Severity,
                isResolved);

            var result = await GetValidationIssueListUseCase.ExecuteAsync(
                new GetValidationIssueListRequestDto
                {
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    Keyword = Keyword,
                    Severity = Severity,
                    IsResolved = isResolved
                });

            Issues = result.Items.ToList();
            TotalItems = result.TotalItems;
            TotalPages = result.TotalPages;
        }
        catch (Exception ex)
        {
            Issues.Clear();
            TotalItems = 0;
            TotalPages = 0;
            ErrorMessage = L["ValidationIssueListLoadFailed"];

            Logger.LogError(ex, "Failed to load validation issues.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string FormatDateTime(DateTimeOffset value)
    {
        var localTime = TimeZoneInfo.ConvertTime(value, UserTimeZone);
        return localTime.ToString("dd/MM/yyyy HH:mm");
    }
}
