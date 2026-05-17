using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.BackgroundJobs;
using OAI.Application.Abstractions.BackgroundJobs.Uploads;
using OAI.Application.Abstractions.Services;
using OAI.Application.Uploads.Dtos;
using OAI.Infrastructure.Identity;
using OAI.Web.Localization;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Mobile;

public partial class MobileCapture
{
    private const long MaxFileSize = 20 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff",
        ".pdf"
    };

    [Inject]
    private IUploadPackageService UploadPackageService { get; set; } = default!;

    [Inject]
    private IBackgroundJobClient BackgroundJobClient { get; set; } = default!;

    [Inject]
    private ICurrentUserContext CurrentUserContext { get; set; } = default!;

    [Inject]
    private ILogger<MobileCapture> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [Inject]
    private CurrentUserAuthorizationService AuthorizationService { get; set; } = default!;

    [Inject]
    private IToastService ToastService { get; set; } = default!;

    private IBrowserFile? SelectedFile { get; set; }

    private string? SelectedFileName { get; set; }

    private string SelectedFileSizeText { get; set; } = string.Empty;

    private bool IsUploading { get; set; }

    private bool CanUpload => SelectedFile is not null && string.IsNullOrWhiteSpace(ErrorMessage);

    private string? ErrorMessage { get; set; }

    private MobileCaptureUploadResult? UploadResult { get; set; }

    private Task HandleFileSelectedAsync(InputFileChangeEventArgs e)
    {
        ResetMessages();

        SelectedFile = e.File;
        UploadResult = null;

        if (SelectedFile is null)
        {
            ErrorMessage = L["MobileCaptureFileRequired"];
            return Task.CompletedTask;
        }

        SelectedFileName = SelectedFile.Name;
        SelectedFileSizeText = FormatFileSize(SelectedFile.Size);

        var extension = Path.GetExtension(SelectedFile.Name);

        if (!AllowedExtensions.Contains(extension))
        {
            ErrorMessage = L["MobileCaptureUnsupportedFileFormat"];
            SelectedFile = null;
            return Task.CompletedTask;
        }

        if (SelectedFile.Size > MaxFileSize)
        {
            ErrorMessage = L["MobileCaptureFileTooLarge"];
            SelectedFile = null;
            return Task.CompletedTask;
        }

        Logger.LogInformation(
            "Mobile invoice file selected. FileName: {FileName}, Size: {FileSize}",
            SelectedFile.Name,
            SelectedFile.Size);

        return Task.CompletedTask;
    }

    private async Task UploadAsync()
    {
        if (!await AuthorizationService.IsAuthorizedAsync(ApplicationPolicies.UploadInvoices))
        {
            ToastService.Error(L["UploadNotAllowed"]);
            return;
        }

        if (SelectedFile is null)
        {
            ErrorMessage = L["MobileCaptureFileRequired"];
            return;
        }

        ResetMessages();
        IsUploading = true;

        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        try
        {
            Logger.LogInformation("Start mobile invoice capture upload. FileName: {FileName}", SelectedFile.Name);

            await using var stream = SelectedFile.OpenReadStream(MaxFileSize);

            var packageResult = await UploadPackageService.CreateAsync(
                new CreateUploadPackageRequestDto(
                    FileName: SelectedFile.Name,
                    ContentType: string.IsNullOrWhiteSpace(SelectedFile.ContentType)
                        ? "application/octet-stream"
                        : SelectedFile.ContentType,
                    FileSizeBytes: SelectedFile.Size,
                    Content: stream,
                    UploadedByUserId: TryParseCurrentUserId(),
                    UploadedByUserName: CurrentUserContext.UserName),
                CancellationToken.None);

            var backgroundJobId = await BackgroundJobClient.EnqueueAsync<IProcessUploadBatchJob>(
                job => job.ProcessAsync(packageResult.UploadBatchId, CancellationToken.None),
                BackgroundJobQueues.Uploads,
                CancellationToken.None);

            UploadResult = new MobileCaptureUploadResult(
                UploadBatchId: packageResult.UploadBatchId,
                BatchCode: packageResult.BatchCode,
                TotalFiles: packageResult.TotalFiles,
                BackgroundJobId: backgroundJobId,
                Status: "Queued");

            ToastService.Success(L["MobileCaptureQueuedMessage"]);

            Logger.LogInformation(
                "Mobile invoice upload package queued. UploadBatchId: {UploadBatchId}, BatchCode: {BatchCode}, BackgroundJobId: {BackgroundJobId}",
                UploadResult.UploadBatchId,
                UploadResult.BatchCode,
                UploadResult.BackgroundJobId);
        }
        catch (Exception ex)
        {
            ToastService.Error(L["InvoiceUploadFailed"]);

            Logger.LogError(
                ex,
                "Mobile invoice capture upload failed. FileName: {FileName}",
                SelectedFile.Name);
        }
        finally
        {
            IsUploading = false;
        }
    }

    private void ResetForm()
    {
        SelectedFile = null;
        SelectedFileName = null;
        SelectedFileSizeText = string.Empty;
        UploadResult = null;
        ResetMessages();
    }

    private void ResetMessages()
    {
        ErrorMessage = null;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024d:N1} KB";

        return $"{bytes / 1024d / 1024d:N1} MB";
    }

    private Guid? TryParseCurrentUserId()
    {
        return Guid.TryParse(CurrentUserContext.UserId, out var userId)
            ? userId
            : null;
    }

    private sealed record MobileCaptureUploadResult(
        Guid UploadBatchId,
        string BatchCode,
        int TotalFiles,
        string BackgroundJobId,
        string Status);
}
