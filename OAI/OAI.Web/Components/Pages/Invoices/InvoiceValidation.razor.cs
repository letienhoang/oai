using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;

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

    protected List<ValidationIssueListItemDto> Issues { get; private set; } = new();

    protected string? Keyword { get; set; }

    protected string? Severity { get; set; }

    protected string? ResolvedFilter { get; set; }

    protected int PageNumber { get; private set; } = 1;

    protected int PageSize { get; private set; } = DefaultPageSize;

    protected int TotalItems { get; private set; }

    protected int TotalPages { get; private set; }

    protected bool IsLoading { get; private set; }

    protected string? ErrorMessage { get; private set; }

    protected bool CanGoPrevious => PageNumber > 1;

    protected bool CanGoNext => PageNumber < TotalPages;

    protected override async Task OnInitializedAsync()
    {
        await LoadIssuesAsync();
    }

    protected async Task ReloadAsync()
    {
        await LoadIssuesAsync();
    }

    protected async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadIssuesAsync();
    }

    protected async Task ClearSearchAsync()
    {
        Keyword = null;
        Severity = null;
        ResolvedFilter = null;
        PageNumber = 1;
        await LoadIssuesAsync();
    }

    protected async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        PageNumber--;
        await LoadIssuesAsync();
    }

    protected async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        PageNumber++;
        await LoadIssuesAsync();
    }

    protected async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    protected void GoToInvoiceDetail(Guid invoiceId)
    {
        NavigationManager.NavigateTo($"/invoices/{invoiceId}");
    }

    protected static string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Chưa có" : value;
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
            ErrorMessage = "Không thể tải danh sách lỗi kiểm tra. Vui lòng kiểm tra log để biết thêm chi tiết.";

            Logger.LogError(ex, "Failed to load validation issues.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}