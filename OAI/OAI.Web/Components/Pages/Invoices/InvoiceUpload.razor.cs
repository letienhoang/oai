using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.BackgroundJobs;
using OAI.Application.Abstractions.BackgroundJobs.Uploads;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Messaging;
using OAI.Application.Uploads.Dtos;
using OAI.Application.Vendors.Dtos;
using OAI.Infrastructure.Identity;
using OAI.Web.Components.Vendors;
using OAI.Web.Components.Shared;
using OAI.Web.Localization;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceUpload
{
    private const long MaxFileSize = 20 * 1024 * 1024; // 20 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".tif",
        ".tiff",
        ".pdf",
        ".zip"
    };

    [Inject]
    private IUploadPackageService UploadPackageService { get; set; } = default!;

    [Inject]
    private IBackgroundJobClient BackgroundJobClient { get; set; } = default!;

    [Inject]
    private ICurrentUserContext CurrentUserContext { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceUpload> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [Inject]
    private LocalizedMessageResolver LocalizedMessageResolver { get; set; } = default!;

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

    private InvoiceUploadResultDto? UploadResult { get; set; }

    private QuickCreateVendorDialog? QuickCreateVendorDialog { get; set; }

    private ConfirmDialog? ConfirmDialog { get; set; }

    private Task HandleFileSelectedAsync(InputFileChangeEventArgs e)
    {
        ResetMessages();

        SelectedFile = e.File;
        UploadResult = null;

        if (SelectedFile is null)
        {
            ErrorMessage = L["PleaseSelectInvoiceFile"];
            return Task.CompletedTask;
        }

        SelectedFileName = SelectedFile.Name;
        SelectedFileSizeText = FormatFileSize(SelectedFile.Size);

        var extension = Path.GetExtension(SelectedFile.Name);

        if (!AllowedExtensions.Contains(extension))
        {
            ErrorMessage = L["UnsupportedInvoiceFileFormat"];
            SelectedFile = null;
            return Task.CompletedTask;
        }

        if (SelectedFile.Size > MaxFileSize)
        {
            ErrorMessage = string.Format(
                CultureInfo.CurrentCulture,
                L["FileSizeExceeded"],
                FormatFileSize(MaxFileSize));
            SelectedFile = null;
            return Task.CompletedTask;
        }

        Logger.LogInformation(
            "Invoice file selected. FileName: {FileName}, Size: {FileSize}",
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
            ErrorMessage = L["PleaseSelectFileBeforeUpload"];
            return;
        }

        ResetMessages();
        IsUploading = true;

        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        try
        {
            Logger.LogInformation("Start uploading invoice from Blazor UI. FileName: {FileName}", SelectedFile.Name);

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

            UploadResult = new InvoiceUploadResultDto
            {
                InvoiceId = Guid.Empty,
                FileName = SelectedFile.Name,
                Status = "Queued",
                Message = $"Upload package {packageResult.BatchCode} was queued for processing.",
                MessageCode = ApplicationMessageCodes.InvoiceFileStored,
                MessageParameters = new Dictionary<string, string>
                {
                    ["BatchCode"] = packageResult.BatchCode,
                    ["TotalFiles"] = packageResult.TotalFiles.ToString(CultureInfo.InvariantCulture),
                    ["BackgroundJobId"] = backgroundJobId
                }
            };

            ToastService.Success(LocalizedMessageResolver.Resolve(
                UploadResult.MessageCode,
                UploadResult.MessageParameters,
                UploadResult.Message));

            Logger.LogInformation(
                "Invoice upload package queued from Blazor UI. FileName: {FileName}, UploadBatchId: {UploadBatchId}, BackgroundJobId: {BackgroundJobId}",
                SelectedFile.Name,
                packageResult.UploadBatchId,
                backgroundJobId);
        }
        catch (Exception ex)
        {
            ToastService.Error(L["InvoiceUploadFailed"]);

            Logger.LogError(
                ex,
                "Invoice upload failed from Blazor UI. FileName: {FileName}",
                SelectedFile.Name);
        }
        finally
        {
            IsUploading = false;
        }
    }

    private async Task ConfirmUpload()
    {
        if (SelectedFile is null)
        {
            ErrorMessage = L["PleaseSelectFileBeforeUpload"];
            return;
        }

        if (ConfirmDialog is null)
            return;

        var confirmed = await ConfirmDialog.ShowAsync(
            title: L["ConfirmUploadInvoiceTitle"],
            message: L["ConfirmUploadInvoiceMessage"],
            confirmText: L["UploadAndProcess"],
            cancelText: L["Cancel"],
            confirmButtonClass: "btn btn-primary");

        if (!confirmed)
            return;

        await UploadAsync();
    }

    private void ResetForm()
    {
        SelectedFile = null;
        SelectedFileName = null;
        SelectedFileSizeText = string.Empty;
        UploadResult = null;
        ResetMessages();
    }

    private void GoToInvoiceDetail()
    {
        if (UploadResult is null || UploadResult.InvoiceId == Guid.Empty)
            return;

        NavigationManager.NavigateTo($"/invoices/{UploadResult.InvoiceId}");
    }

    private void OpenQuickCreateVendorDialog()
    {
        QuickCreateVendorDialog?.Open();
    }

    private Task HandleUploadVendorCreatedAsync(VendorListItemDto vendor)
    {
        ToastService.Success(string.Format(
            CultureInfo.CurrentCulture,
            L["VendorCreatedSuccessfullyWithName"].Value,
            vendor.Name));

        return Task.CompletedTask;
    }

    private static string GetStatusBadgeClass(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "processed" => "text-bg-success",
            "queued" => "text-bg-info",
            "failed" => "text-bg-danger",
            _ => "text-bg-secondary"
        };
    }

    private void ResetMessages()
    {
        ErrorMessage = null;
    }

    private string LocalizeMessage(
        string? messageCode,
        IReadOnlyDictionary<string, string>? parameters,
        string? fallbackMessage)
    {
        return LocalizedMessageResolver.Resolve(messageCode, parameters, fallbackMessage);
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
}
