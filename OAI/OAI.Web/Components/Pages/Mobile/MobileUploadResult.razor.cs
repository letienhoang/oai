using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OAI.Web.Localization;
using OAI.Web.Services.Uploads;

namespace OAI.Web.Components.Pages.Mobile;

public partial class MobileUploadResult : IDisposable
{
    private CancellationTokenSource? _pollingCancellationTokenSource;

    [Parameter]
    public Guid BatchId { get; set; }

    [Inject]
    private IMobileUploadApiClient MobileUploadApiClient { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<MobileUploadResult> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    private MobileUploadBatchStatusResponse? Batch { get; set; }

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    private int CompletedCount =>
        (Batch?.ProcessedFiles ?? 0) + (Batch?.FailedFiles ?? 0) + (Batch?.UnsupportedFiles ?? 0);

    private int ProgressPercent =>
        Batch is null || Batch.TotalFiles == 0
            ? 0
            : CompletedCount * 100 / Batch.TotalFiles;

    protected override async Task OnInitializedAsync()
    {
        await LoadBatchAsync();
        StartPollingIfNeeded();
    }

    private async Task ReloadAsync()
    {
        await LoadBatchAsync();
        StartPollingIfNeeded();
    }

    private async Task LoadBatchAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            Batch = await MobileUploadApiClient.GetBatchStatusAsync(BatchId, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = L["MobileUploadResultLoadFailed"];

            Logger.LogWarning(
                ex,
                "Failed to load mobile upload result. UploadBatchId: {UploadBatchId}",
                BatchId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void StartPollingIfNeeded()
    {
        _pollingCancellationTokenSource?.Cancel();
        _pollingCancellationTokenSource?.Dispose();
        _pollingCancellationTokenSource = null;

        if (Batch is null || IsFinalStatus(Batch.Status))
            return;

        _pollingCancellationTokenSource = new CancellationTokenSource();
        _ = PollAsync(_pollingCancellationTokenSource.Token);
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(4));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var latest = await MobileUploadApiClient.GetBatchStatusAsync(BatchId, cancellationToken);

                await InvokeAsync(() =>
                {
                    Batch = latest;
                    ErrorMessage = null;
                    StateHasChanged();
                });

                if (IsFinalStatus(latest.Status))
                    break;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Mobile upload result polling failed. UploadBatchId: {UploadBatchId}",
                BatchId);
        }
    }

    private void BackToCapture()
    {
        NavigationManager.NavigateTo("/mobile/capture");
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    private static bool IsFinalStatus(string status)
    {
        return status.ToLowerInvariant() is
            "processed" or
            "completed" or
            "succeeded" or
            "failed" or
            "partiallyfailed" or
            "unsupported";
    }

    private static string GetStatusBadgeClass(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "processed" or "completed" or "succeeded" => "text-bg-success",
            "processing" => "text-bg-primary",
            "queued" or "created" => "text-bg-info",
            "retrypending" => "text-bg-warning",
            "partiallyfailed" => "text-bg-warning",
            "failed" => "text-bg-danger",
            "unsupported" => "text-bg-secondary",
            _ => "text-bg-secondary"
        };
    }

    public void Dispose()
    {
        _pollingCancellationTokenSource?.Cancel();
        _pollingCancellationTokenSource?.Dispose();
    }
}
