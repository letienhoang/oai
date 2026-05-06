using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Audit;
using OAI.Application.Audit.Dtos;
using OAI.Web.Components.Audit;
using OAI.Web.Localization;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Audit;

public partial class AuditLogs
{
    private const int DefaultPageSize = 10;

    [Inject]
    private IGetAuditLogListUseCase GetAuditLogListUseCase { get; set; } = default!;

    [Inject]
    private IGetAuditLogFilterOptionsUseCase GetAuditLogFilterOptionsUseCase { get; set; } = default!;

    [Inject]
    private UserTimeZoneService UserTimeZoneService { get; set; } = default!;

    [Inject]
    private ILogger<AuditLogs> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    private List<AuditLogListItemDto> Logs { get; set; } = new();

    private AuditLogDetailDialog? AuditLogDetailDialog { get; set; }

    private string? Keyword { get; set; }

    private string? SelectedEntityName { get; set; }

    private string? SelectedActionType { get; set; }

    private string? SelectedSource { get; set; }

    private string? UserName { get; set; }

    private DateOnly? OccurredAtFrom { get; set; }

    private DateOnly? OccurredAtTo { get; set; }

    private List<string> EntityNameOptions { get; set; } = new();

    private List<string> ActionTypeOptions { get; set; } = new();

    private List<string> SourceOptions { get; set; } = new();

    private string? FilterErrorMessage { get; set; }

    private int PageNumber { get; set; } = 1;

    private int PageSize { get; set; } = DefaultPageSize;

    private int TotalItems { get; set; }

    private int TotalPages { get; set; }

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    private TimeZoneInfo UserTimeZone { get; set; } = TimeZoneInfo.Utc;

    private bool CanGoPrevious => PageNumber > 1;

    private bool CanGoNext => PageNumber < TotalPages;

    protected override async Task OnInitializedAsync()
    {
        await LoadFilterOptionsAsync();
        await LoadLogsAsync();
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
        await LoadLogsAsync();
    }

    private async Task ApplyFilterAsync()
    {
        FilterErrorMessage = null;

        if (OccurredAtFrom.HasValue
            && OccurredAtTo.HasValue
            && OccurredAtFrom.Value > OccurredAtTo.Value)
        {
            FilterErrorMessage = L["InvalidDateRange"];
            return;
        }

        PageNumber = 1;
        AuditLogDetailDialog?.Close();
        await LoadLogsAsync();
    }

    private async Task ClearFilterAsync()
    {
        Keyword = null;
        SelectedEntityName = null;
        SelectedActionType = null;
        SelectedSource = null;
        UserName = null;
        OccurredAtFrom = null;
        OccurredAtTo = null;
        FilterErrorMessage = null;
        PageNumber = 1;
        AuditLogDetailDialog?.Close();
        await LoadLogsAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        PageNumber--;
        AuditLogDetailDialog?.Close();
        await LoadLogsAsync();
    }

    private async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        PageNumber++;
        AuditLogDetailDialog?.Close();
        await LoadLogsAsync();
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilterAsync();
        }
    }

    private async Task LoadFilterOptionsAsync()
    {
        try
        {
            var options = await GetAuditLogFilterOptionsUseCase.ExecuteAsync();

            EntityNameOptions = options.EntityNames.ToList();
            ActionTypeOptions = options.ActionTypes.ToList();
            SourceOptions = options.Sources.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load audit log filter options.");
        }
    }

    private AuditLogFilterDto BuildFilter()
    {
        return new AuditLogFilterDto
        {
            Keyword = Keyword,
            EntityName = SelectedEntityName,
            ActionType = SelectedActionType,
            UserName = UserName,
            Source = SelectedSource,
            OccurredAtFrom = OccurredAtFrom,
            OccurredAtTo = OccurredAtTo
        };
    }

    private void OpenAuditLogDetail(AuditLogListItemDto auditLog)
    {
        AuditLogDetailDialog?.Open(auditLog);
    }

    private async Task LoadLogsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Logger.LogInformation(
                "Loading audit logs. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}, EntityName: {EntityName}, ActionType: {ActionType}, UserName: {UserName}, Source: {Source}, OccurredAtFrom: {OccurredAtFrom}, OccurredAtTo: {OccurredAtTo}",
                PageNumber,
                PageSize,
                Keyword,
                SelectedEntityName,
                SelectedActionType,
                UserName,
                SelectedSource,
                OccurredAtFrom,
                OccurredAtTo);

            var result = await GetAuditLogListUseCase.ExecuteAsync(
                new GetAuditLogListRequestDto
                {
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    Filter = BuildFilter()
                });

            Logs = result.Items.ToList();
            TotalItems = result.TotalItems;
            TotalPages = result.TotalPages;
        }
        catch (Exception ex)
        {
            Logs.Clear();
            TotalItems = 0;
            TotalPages = 0;
            AuditLogDetailDialog?.Close();
            ErrorMessage = L["AuditLogsLoadFailed"];

            Logger.LogError(ex, "Failed to load audit logs.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string FormatDateTime(DateTimeOffset value)
    {
        var localTime = TimeZoneInfo.ConvertTime(value, UserTimeZone);
        return localTime.ToString("dd/MM/yyyy HH:mm:ss");
    }

    private string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? L["NotAvailable"] : value;
    }

    private string DisplayAuditUserName(AuditLogListItemDto log)
    {
        return string.IsNullOrWhiteSpace(log.UserName)
            ? L["SystemUser"]
            : log.UserName;
    }

    private static string GetActionBadgeClass(string actionType)
    {
        return actionType.ToLowerInvariant() switch
        {
            "created" => "text-bg-success",
            "updated" => "text-bg-primary",
            "deleted" => "text-bg-danger",
            "uploaded" => "text-bg-info",
            "processed" => "text-bg-info",
            "validated" => "text-bg-warning",
            "exported" => "text-bg-secondary",
            _ => "text-bg-light border text-dark"
        };
    }

}
