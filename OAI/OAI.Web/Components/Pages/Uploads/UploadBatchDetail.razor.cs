using Microsoft.AspNetCore.Components;
using OAI.Application.Abstractions.Persistence;
using OAI.Domain.Entities;
using OAI.Domain.Enums;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Uploads;

public partial class UploadBatchDetail : ComponentBase
{
    [Parameter]
    public Guid BatchId { get; set; }

    [Inject]
    private IUploadBatchRepository UploadBatchRepository { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<UploadBatchDetail> Logger { get; set; } = default!;

    [Inject]
    private UserTimeZoneService UserTimeZoneService { get; set; } = default!;

    private UploadBatch? Batch { get; set; }

    private TimeZoneInfo UserTimeZone { get; set; } = TimeZoneInfo.Utc;

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    private int PendingFiles =>
        Batch?.Files.Count(x => x.Status is UploadBatchFileStatus.Created or UploadBatchFileStatus.Queued) ?? 0;

    private int ProcessingFiles =>
        Batch?.Files.Count(x => x.Status == UploadBatchFileStatus.Processing) ?? 0;

    private int RetryPendingFiles =>
        Batch?.Files.Count(x => x.Status == UploadBatchFileStatus.RetryPending) ?? 0;

    protected override async Task OnParametersSetAsync()
    {
        await LoadBatchAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        UserTimeZone = await UserTimeZoneService.GetUserTimeZoneAsync();
        StateHasChanged();
    }

    private async Task LoadBatchAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            Logger.LogInformation(
                "Loading upload batch detail. UploadBatchId: {UploadBatchId}",
                BatchId);

            Batch = await UploadBatchRepository.GetByIdWithFilesAsync(BatchId);

            if (Batch is null)
            {
                Logger.LogWarning(
                    "Upload batch detail not found. UploadBatchId: {UploadBatchId}",
                    BatchId);
            }
        }
        catch (Exception ex)
        {
            Batch = null;
            ErrorMessage = "Failed to load upload batch detail.";

            Logger.LogError(
                ex,
                "Failed to load upload batch detail. UploadBatchId: {UploadBatchId}",
                BatchId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/invoices");
    }

    private void GoToInvoice(Guid invoiceId)
    {
        NavigationManager.NavigateTo($"/invoices/{invoiceId}");
    }

    private string FormatDateTime(DateTimeOffset value)
    {
        var localTime = TimeZoneInfo.ConvertTime(value, UserTimeZone);
        return localTime.ToString("dd/MM/yyyy HH:mm");
    }

    private string FormatDateTimeOrFallback(DateTimeOffset? value)
    {
        return value is null
            ? "-"
            : FormatDateTime(value.Value);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes:N0} B";

        var kilobytes = bytes / 1024d;
        if (kilobytes < 1024)
            return $"{kilobytes:N1} KB";

        var megabytes = kilobytes / 1024d;
        if (megabytes < 1024)
            return $"{megabytes:N1} MB";

        var gigabytes = megabytes / 1024d;
        return $"{gigabytes:N1} GB";
    }

    private static string GetBatchStatusBadgeClass(UploadBatchStatus status)
    {
        return status switch
        {
            UploadBatchStatus.Created => "text-bg-secondary",
            UploadBatchStatus.Queued => "text-bg-warning",
            UploadBatchStatus.Processing => "text-bg-info",
            UploadBatchStatus.Completed => "text-bg-success",
            UploadBatchStatus.PartiallyFailed => "text-bg-warning",
            UploadBatchStatus.Failed => "text-bg-danger",
            UploadBatchStatus.Cancelled => "text-bg-secondary",
            _ => "text-bg-secondary"
        };
    }

    private static string GetFileStatusBadgeClass(UploadBatchFileStatus status)
    {
        return status switch
        {
            UploadBatchFileStatus.Created => "text-bg-secondary",
            UploadBatchFileStatus.Queued => "text-bg-warning",
            UploadBatchFileStatus.Processing => "text-bg-info",
            UploadBatchFileStatus.Processed => "text-bg-success",
            UploadBatchFileStatus.Failed => "text-bg-danger",
            UploadBatchFileStatus.Skipped => "text-bg-secondary",
            UploadBatchFileStatus.Unsupported => "text-bg-dark",
            UploadBatchFileStatus.RetryPending => "text-bg-warning",
            _ => "text-bg-secondary"
        };
    }
}