using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Audit;
using OAI.Application.Audit.Dtos;
using OAI.Web.Localization;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Audit;

public partial class AuditLogs
{
    private const int DefaultPageSize = 10;

    [Inject]
    private IGetAuditLogListUseCase GetAuditLogListUseCase { get; set; } = default!;

    [Inject]
    private UserTimeZoneService UserTimeZoneService { get; set; } = default!;

    [Inject]
    private ILogger<AuditLogs> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    private List<AuditLogListItemDto> Logs { get; set; } = new();

    private AuditLogListItemDto? SelectedLog { get; set; }

    private string? Keyword { get; set; }

    private string? EntityName { get; set; }

    private string? ActionType { get; set; }

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

    private async Task SearchAsync()
    {
        PageNumber = 1;
        SelectedLog = null;
        await LoadLogsAsync();
    }

    private async Task ClearSearchAsync()
    {
        Keyword = null;
        EntityName = null;
        ActionType = null;
        PageNumber = 1;
        SelectedLog = null;
        await LoadLogsAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        PageNumber--;
        SelectedLog = null;
        await LoadLogsAsync();
    }

    private async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        PageNumber++;
        SelectedLog = null;
        await LoadLogsAsync();
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    private void SelectLog(AuditLogListItemDto log)
    {
        SelectedLog = log;
    }

    private void CloseSelectedLog()
    {
        SelectedLog = null;
    }

    private async Task LoadLogsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Logger.LogInformation(
                "Loading audit logs. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}, EntityName: {EntityName}, ActionType: {ActionType}",
                PageNumber,
                PageSize,
                Keyword,
                EntityName,
                ActionType);

            var result = await GetAuditLogListUseCase.ExecuteAsync(
                new GetAuditLogListRequestDto
                {
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    Keyword = Keyword,
                    EntityName = EntityName,
                    ActionType = ActionType
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
            SelectedLog = null;
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

    private string FormatJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return L["NotAvailable"];

        try
        {
            using var document = JsonDocument.Parse(json);

            return JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }
        catch
        {
            return json;
        }
    }
}
