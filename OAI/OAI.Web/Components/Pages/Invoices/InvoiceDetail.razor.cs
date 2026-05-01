using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Web.Localization;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceDetail
{
    [Parameter]
    public Guid InvoiceId { get; set; }

    [Inject]
    private IGetInvoiceDetailUseCase GetInvoiceDetailUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceDetail> Logger { get; set; } = default!;

    [Inject]
    private UserTimeZoneService UserTimeZoneService { get; set; } = default!;

    [Inject]
    private IApproveInvoiceUseCase ApproveInvoiceUseCase { get; set; } = default!;

    [Inject]
    private IRejectInvoiceUseCase RejectInvoiceUseCase { get; set; } = default!;

    [Inject]
    private IMoveInvoiceToPendingReviewUseCase MoveInvoiceToPendingReviewUseCase { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    private InvoiceDetailDto? Invoice { get; set; }

    private TimeZoneInfo UserTimeZone { get; set; } = TimeZoneInfo.Utc;

    private bool IsLoading { get; set; }

    private string? ErrorMessage { get; set; }

    private bool IsApproving { get; set; }

    private string? SuccessMessage { get; set; }

    private string? ActionErrorMessage { get; set; }

    private bool CanApprove =>
        Invoice is not null &&
        string.Equals(Invoice.Status, "PendingReview", StringComparison.OrdinalIgnoreCase) &&
        !Invoice.ValidationIssues.Any(x =>
            string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase) &&
            !x.IsResolved);

    private bool IsRejecting { get; set; }

    private bool IsMovingToPendingReview { get; set; }

    private bool CanReject =>
        Invoice is not null &&
        !string.Equals(Invoice.Status, "Rejected", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Invoice.Status, "Exported", StringComparison.OrdinalIgnoreCase);

    private bool CanMoveToPendingReview =>
        Invoice is not null &&
        !string.Equals(Invoice.Status, "PendingReview", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Invoice.Status, "Exported", StringComparison.OrdinalIgnoreCase);

    protected override async Task OnParametersSetAsync()
    {
        await LoadInvoiceDetailAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        UserTimeZone = await UserTimeZoneService.GetUserTimeZoneAsync();
        StateHasChanged();
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/invoices");
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        return $"{amount:N0} {currency}";
    }

    private string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? L["NotAvailable"] : value;
    }

    private string FormatDateTime(DateTimeOffset value)
    {
        var localTime = TimeZoneInfo.ConvertTime(value, UserTimeZone);
        return localTime.ToString("dd/MM/yyyy HH:mm");
    }

    private static string GetStatusBadgeClass(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "draft" => "text-bg-secondary",
            "pendingreview" => "text-bg-warning",
            "approved" => "text-bg-success",
            "rejected" => "text-bg-danger",
            "exported" => "text-bg-info",
            _ => "text-bg-secondary"
        };
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

    private void GoToEdit()
    {
        NavigationManager.NavigateTo($"/invoices/{InvoiceId}/edit");
    }

    private void GoToCompare()
    {
        NavigationManager.NavigateTo($"/invoices/{InvoiceId}/compare");
    }

    private async Task LoadInvoiceDetailAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            Logger.LogInformation("Loading invoice detail. InvoiceId: {InvoiceId}", InvoiceId);

            Invoice = await GetInvoiceDetailUseCase.ExecuteAsync(
                new GetInvoiceDetailRequestDto
                {
                    InvoiceId = InvoiceId
                });

            Logger.LogInformation(
                "Invoice detail loaded. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
                Invoice.InvoiceId,
                Invoice.InvoiceNumber);
        }
        catch (Exception ex)
        {
            Invoice = null;
            ErrorMessage = L["InvoiceDetailLoadFailed"];

            Logger.LogError(ex, "Failed to load invoice detail. InvoiceId: {InvoiceId}", InvoiceId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ApproveAsync()
    {
        if (Invoice is null)
            return;

        SuccessMessage = null;
        ActionErrorMessage = null;
        IsApproving = true;

        try
        {
            Logger.LogInformation(
                "Approving invoice from detail page. InvoiceId: {InvoiceId}",
                Invoice.InvoiceId);

            var result = await ApproveInvoiceUseCase.ExecuteAsync(
                new ApproveInvoiceRequestDto
                {
                    InvoiceId = Invoice.InvoiceId
                });

            SuccessMessage = L["InvoiceApprovedSuccessfully"];

            Logger.LogInformation(
                "Invoice approved from detail page. InvoiceId: {InvoiceId}, Status: {Status}",
                result.InvoiceId,
                result.Status);

            await LoadInvoiceDetailAsync();
        }
        catch (Exception ex)
        {
            ActionErrorMessage = L["InvoiceApproveFailed"];

            Logger.LogError(
                ex,
                "Failed to approve invoice from detail page. InvoiceId: {InvoiceId}",
                Invoice.InvoiceId);
        }
        finally
        {
            IsApproving = false;
        }
    }

    private async Task RejectAsync()
    {
        if (Invoice is null)
            return;

        SuccessMessage = null;
        ActionErrorMessage = null;
        IsRejecting = true;

        try
        {
            Logger.LogInformation(
                "Rejecting invoice from detail page. InvoiceId: {InvoiceId}",
                Invoice.InvoiceId);

            var result = await RejectInvoiceUseCase.ExecuteAsync(
                new RejectInvoiceRequestDto
                {
                    InvoiceId = Invoice.InvoiceId
                });

            SuccessMessage = L["InvoiceRejectedSuccessfully"];

            Logger.LogInformation(
                "Invoice rejected from detail page. InvoiceId: {InvoiceId}, Status: {Status}",
                result.InvoiceId,
                result.Status);

            await LoadInvoiceDetailAsync();
        }
        catch (Exception ex)
        {
            ActionErrorMessage = L["InvoiceRejectFailed"];

            Logger.LogError(
                ex,
                "Failed to reject invoice from detail page. InvoiceId: {InvoiceId}",
                Invoice.InvoiceId);
        }
        finally
        {
            IsRejecting = false;
        }
    }

    private async Task MoveToPendingReviewAsync()
    {
        if (Invoice is null)
            return;

        SuccessMessage = null;
        ActionErrorMessage = null;
        IsMovingToPendingReview = true;

        try
        {
            Logger.LogInformation(
                "Moving invoice to pending review from detail page. InvoiceId: {InvoiceId}",
                Invoice.InvoiceId);

            var result = await MoveInvoiceToPendingReviewUseCase.ExecuteAsync(
                new MoveInvoiceToPendingReviewRequestDto
                {
                    InvoiceId = Invoice.InvoiceId
                });

            SuccessMessage = L["InvoiceMovedToPendingReviewSuccessfully"];

            Logger.LogInformation(
                "Invoice moved to pending review from detail page. InvoiceId: {InvoiceId}, Status: {Status}",
                result.InvoiceId,
                result.Status);

            await LoadInvoiceDetailAsync();
        }
        catch (Exception ex)
        {
            ActionErrorMessage = L["InvoiceMoveToPendingReviewFailed"];

            Logger.LogError(
                ex,
                "Failed to move invoice to pending review from detail page. InvoiceId: {InvoiceId}",
                Invoice.InvoiceId);
        }
        finally
        {
            IsMovingToPendingReview = false;
        }
    }

    private bool CanShowApproveButton =>
        Invoice is not null &&
        string.Equals(Invoice.Status, "PendingReview", StringComparison.OrdinalIgnoreCase) &&
        !Invoice.ValidationIssues.Any(x =>
            string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase) &&
            !x.IsResolved);

    private bool CanShowRejectButton =>
        Invoice is not null &&
        !string.Equals(Invoice.Status, "Rejected", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Invoice.Status, "Exported", StringComparison.OrdinalIgnoreCase);

    private bool CanShowMoveToPendingReviewButton =>
        Invoice is not null &&
        !string.Equals(Invoice.Status, "PendingReview", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Invoice.Status, "Exported", StringComparison.OrdinalIgnoreCase);

    private bool CanShowEditButton =>
        Invoice is not null &&
        !string.Equals(Invoice.Status, "Exported", StringComparison.OrdinalIgnoreCase);
}
