using Microsoft.AspNetCore.Components;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Web.Components.Pages.Invoices.Models;

namespace OAI.Web.Components.Pages.Invoices;

public partial class InvoiceEdit
{
    [Parameter]
    public Guid InvoiceId { get; set; }

    [Inject]
    private IGetInvoiceDetailUseCase GetInvoiceDetailUseCase { get; set; } = default!;

    [Inject]
    private IUpdateInvoiceUseCase UpdateInvoiceUseCase { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<InvoiceEdit> Logger { get; set; } = default!;

    protected InvoiceEditFormModel EditModel { get; private set; } = new();

    private bool IsLoading { get; set; }

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private string? FormErrorMessage { get; set; }

    private string? SuccessMessage { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await LoadInvoiceAsync();
    }

    protected async Task SaveAsync()
    {
        FormErrorMessage = null;
        SuccessMessage = null;

        if (!Guid.TryParse(EditModel.VendorIdText, out var vendorId))
        {
            FormErrorMessage = "VendorId không hợp lệ.";
            return;
        }

        if (EditModel.LineItems.Count == 0)
        {
            FormErrorMessage = "Hóa đơn phải có ít nhất một dòng hàng.";
            return;
        }

        IsSaving = true;

        try
        {
            Logger.LogInformation("Start saving invoice edit form. InvoiceId: {InvoiceId}", EditModel.InvoiceId);

            var request = new InvoiceUpdateRequestDto
            {
                InvoiceId = EditModel.InvoiceId,
                VendorId = vendorId,
                InvoiceNumber = EditModel.InvoiceNumber,
                IssueDate = EditModel.IssueDate,
                DueDate = EditModel.DueDate,
                Currency = EditModel.Currency,
                DeclaredSubtotal = EditModel.DeclaredSubtotal,
                DeclaredTaxAmount = EditModel.DeclaredTaxAmount,
                DeclaredTotalAmount = EditModel.DeclaredTotalAmount,
                LineItems = EditModel.LineItems
                    .OrderBy(x => x.LineNo)
                    .Select(x => new InvoiceLineItemRequestDto
                    {
                        LineNo = x.LineNo,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        TaxRate = x.TaxRate
                    })
                    .ToList()
            };

            var updated = await UpdateInvoiceUseCase.ExecuteAsync(request);

            SuccessMessage = "Lưu hóa đơn thành công. Hệ thống đã kiểm tra lại tính nhất quán dữ liệu.";

            Logger.LogInformation(
                "Invoice edit form saved successfully. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
                updated.InvoiceId,
                updated.InvoiceNumber);

            NavigationManager.NavigateTo($"/invoices/{updated.InvoiceId}");
        }
        catch (Exception ex)
        {
            FormErrorMessage = "Không thể lưu hóa đơn. Vui lòng kiểm tra dữ liệu hoặc xem log để biết thêm chi tiết.";

            Logger.LogError(ex, "Failed to save invoice edit form. InvoiceId: {InvoiceId}", EditModel.InvoiceId);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void AddLineItem()
    {
        var nextLineNo = EditModel.LineItems.Count == 0
            ? 1
            : EditModel.LineItems.Max(x => x.LineNo) + 1;

        EditModel.LineItems.Add(new InvoiceLineItemEditFormModel
        {
            LineNo = nextLineNo,
            Description = string.Empty,
            Quantity = 1,
            UnitPrice = 0,
            TaxRate = 0
        });
    }

    private void RemoveLineItem(int index)
    {
        if (index < 0 || index >= EditModel.LineItems.Count)
            return;

        if (EditModel.LineItems.Count <= 1)
            return;

        EditModel.LineItems.RemoveAt(index);
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo($"/invoices/{InvoiceId}");
    }

    private async Task LoadInvoiceAsync()
    {
        ErrorMessage = null;
        FormErrorMessage = null;
        SuccessMessage = null;
        IsLoading = true;

        try
        {
            Logger.LogInformation("Loading invoice for edit. InvoiceId: {InvoiceId}", InvoiceId);

            var invoice = await GetInvoiceDetailUseCase.ExecuteAsync(
                new GetInvoiceDetailRequestDto
                {
                    InvoiceId = InvoiceId
                });

            EditModel = new InvoiceEditFormModel
            {
                InvoiceId = invoice.InvoiceId,
                VendorIdText = invoice.VendorId.ToString(),
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                Currency = invoice.Currency,
                DeclaredSubtotal = invoice.DeclaredSubtotal,
                DeclaredTaxAmount = invoice.DeclaredTaxAmount,
                DeclaredTotalAmount = invoice.DeclaredTotalAmount,
                LineItems = invoice.LineItems
                    .OrderBy(x => x.LineNo)
                    .Select(x => new InvoiceLineItemEditFormModel
                    {
                        LineNo = x.LineNo,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        TaxRate = x.TaxRate
                    })
                    .ToList()
            };

            if (EditModel.LineItems.Count == 0)
            {
                AddLineItem();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Không thể tải dữ liệu hóa đơn để chỉnh sửa.";

            Logger.LogError(ex, "Failed to load invoice for edit. InvoiceId: {InvoiceId}", InvoiceId);
        }
        finally
        {
            IsLoading = false;
        }
    }
}