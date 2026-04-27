using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAI.Application.Abstractions.Services;
using OAI.Application.Invoices.Dtos;

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
        ".tiff"
    };

    [Inject]
    private IInvoiceProcessingService InvoiceProcessingService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceUpload> Logger { get; set; } = default!;

    private IBrowserFile? SelectedFile { get; set; }

    private string? SelectedFileName { get; set; }

    private string SelectedFileSizeText { get; set; } = string.Empty;

    private bool IsUploading { get; set; }

    private bool CanUpload => SelectedFile is not null && string.IsNullOrWhiteSpace(ErrorMessage);

    private string? ErrorMessage { get; set; }

    private string? SuccessMessage { get; set; }

    private InvoiceUploadResultDto? UploadResult { get; set; }

    protected Task HandleFileSelectedAsync(InputFileChangeEventArgs e)
    {
        ResetMessages();

        SelectedFile = e.File;
        UploadResult = null;

        if (SelectedFile is null)
        {
            ErrorMessage = "Vui lòng chọn một file hóa đơn.";
            return Task.CompletedTask;
        }

        SelectedFileName = SelectedFile.Name;
        SelectedFileSizeText = FormatFileSize(SelectedFile.Size);

        var extension = Path.GetExtension(SelectedFile.Name);

        if (!AllowedExtensions.Contains(extension))
        {
            ErrorMessage = "Định dạng file chưa được hỗ trợ. Vui lòng chọn JPG, JPEG, PNG, TIF hoặc TIFF.";
            SelectedFile = null;
            return Task.CompletedTask;
        }

        if (SelectedFile.Size > MaxFileSize)
        {
            ErrorMessage = $"File vượt quá dung lượng cho phép ({FormatFileSize(MaxFileSize)}).";
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
        if (SelectedFile is null)
        {
            ErrorMessage = "Vui lòng chọn file trước khi upload.";
            return;
        }

        ResetMessages();
        IsUploading = true;

        try
        {
            Logger.LogInformation("Start uploading invoice from Blazor UI. FileName: {FileName}", SelectedFile.Name);

            await using var stream = SelectedFile.OpenReadStream(MaxFileSize);

            UploadResult = await InvoiceProcessingService.UploadInvoiceAsync(
                SelectedFile.Name,
                stream,
                CancellationToken.None);

            if (UploadResult.Status.Equals("Processed", StringComparison.OrdinalIgnoreCase))
            {
                SuccessMessage = "Upload và xử lý hóa đơn thành công.";
            }
            else
            {
                ErrorMessage = UploadResult.Message;
            }

            Logger.LogInformation(
                "Invoice upload completed from Blazor UI. FileName: {FileName}, Status: {Status}, InvoiceId: {InvoiceId}",
                SelectedFile.Name,
                UploadResult.Status,
                UploadResult.InvoiceId);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Đã xảy ra lỗi khi upload và xử lý hóa đơn. Vui lòng kiểm tra log để biết thêm chi tiết.";

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

    private static string GetStatusBadgeClass(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "processed" => "text-bg-success",
            "failed" => "text-bg-danger",
            _ => "text-bg-secondary"
        };
    }

    private void ResetMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024 * 1024)
            return $"{bytes / 1024d:N1} KB";

        return $"{bytes / 1024d / 1024d:N1} MB";
    }
}