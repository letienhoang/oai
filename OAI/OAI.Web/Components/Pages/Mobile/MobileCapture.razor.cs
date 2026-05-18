using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.Services;
using OAI.Infrastructure.Identity;
using OAI.Web.Localization;
using OAI.Web.Services;
using OAI.Web.Services.Uploads;

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
    private IMobileUploadApiClient MobileUploadApiClient { get; set; } = default!;

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

            var response = await MobileUploadApiClient.UploadAsync(
                SelectedFile,
                MaxFileSize,
                CurrentUserContext.UserId,
                CurrentUserContext.UserName,
                CancellationToken.None);

            UploadResult = new MobileCaptureUploadResult(
                UploadBatchId: response.UploadBatchId,
                BatchCode: response.BatchCode,
                TotalFiles: response.TotalFiles,
                BackgroundJobId: response.BackgroundJobId ?? "N/A",
                Status: response.Status);

            ToastService.Success(L["MobileCaptureQueuedMessage"]);

            Logger.LogInformation(
                "Mobile invoice upload queued through OAI.Api. UploadBatchId: {UploadBatchId}, BatchCode: {BatchCode}, BackgroundJobId: {BackgroundJobId}",
                UploadResult.UploadBatchId,
                UploadResult.BatchCode,
                UploadResult.BackgroundJobId);
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = ex.Message.Contains("not allowed", StringComparison.OrdinalIgnoreCase)
                ? L["MobileCaptureApiForbidden"]
                : L["MobileCaptureApiUnauthorized"];

            ToastService.Error(ErrorMessage);

            Logger.LogWarning(
                ex,
                "Mobile invoice capture upload was rejected by OAI.Api. FileName: {FileName}",
                SelectedFile.Name);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message)
                ? L["MobileCaptureApiUploadFailed"]
                : ex.Message;

            ToastService.Error(ErrorMessage);

            Logger.LogWarning(
                ex,
                "Mobile invoice capture API upload failed. FileName: {FileName}",
                SelectedFile.Name);
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

    private sealed record MobileCaptureUploadResult(
        Guid UploadBatchId,
        string BatchCode,
        int TotalFiles,
        string BackgroundJobId,
        string Status);
}
