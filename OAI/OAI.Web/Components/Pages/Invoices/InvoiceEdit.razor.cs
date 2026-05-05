using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Invoices.Dtos;
using OAI.Infrastructure.Identity;
using OAI.Web.Components.Pages.Invoices.Models;
using OAI.Web.Localization;
using OAI.Web.Services;
using System.Globalization;

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

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [Inject]
    private CurrentUserAuthorizationService AuthorizationService { get; set; } = default!;

    protected InvoiceEditFormModel EditModel { get; private set; } = new();

    private EditContext EditContext { get; set; } = default!;

    private ValidationMessageStore ValidationMessageStore { get; set; } = default!;

    private bool IsLoading { get; set; }

    private bool IsSaving { get; set; }

    private string? ErrorMessage { get; set; }

    private string? FormErrorMessage { get; set; }

    private string? SuccessMessage { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        EnsureEditContext();

        await LoadInvoiceAsync();
    }

    protected async Task SaveAsync()
    {
        FormErrorMessage = null;
        SuccessMessage = null;

        if (!await AuthorizationService.IsAuthorizedAsync(ApplicationPolicies.EditInvoices))
        {
            FormErrorMessage = L["EditInvoiceNotAllowed"];
            return;
        }

        if (!Guid.TryParse(EditModel.VendorIdText, out var vendorId))
        {
            FormErrorMessage = L["InvalidVendorId"];
            return;
        }

        if (EditModel.LineItems.Count == 0)
        {
            FormErrorMessage = L["InvoiceMustHaveAtLeastOneLineItem"];
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
                        InvoiceLineItemId = x.InvoiceLineItemId,
                        LineNo = x.LineNo,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        TaxRate = x.TaxRate
                    })
                    .ToList()
            };

            var updated = await UpdateInvoiceUseCase.ExecuteAsync(request);

            SuccessMessage = L["InvoiceSavedAndRevalidatedSuccessfully"];

            Logger.LogInformation(
                "Invoice edit form saved successfully. InvoiceId: {InvoiceId}, InvoiceNumber: {InvoiceNumber}",
                updated.InvoiceId,
                updated.InvoiceNumber);

            NavigationManager.NavigateTo($"/invoices/{updated.InvoiceId}");
        }
        catch (Exception ex)
        {
            FormErrorMessage = L["InvoiceSaveFailed"];

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
            InvoiceLineItemId = null,
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
                        InvoiceLineItemId = x.InvoiceLineItemId,
                        LineNo = x.LineNo,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        TaxRate = x.TaxRate
                    })
                    .ToList()
            };
            ConfigureEditContext();

            if (EditModel.LineItems.Count == 0)
            {
                AddLineItem();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = L["InvoiceEditLoadFailed"];

            Logger.LogError(ex, "Failed to load invoice for edit. InvoiceId: {InvoiceId}", InvoiceId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void EnsureEditContext()
    {
        if (EditContext is null)
        {
            ConfigureEditContext();
        }
    }

    private void ConfigureEditContext()
    {
        EditContext = new EditContext(EditModel);
        ValidationMessageStore = new ValidationMessageStore(EditContext);

        EditContext.OnValidationRequested += (_, _) => ValidateEditModel();
        EditContext.OnFieldChanged += (_, _) =>
        {
            ValidateEditModel();
            EditContext.NotifyValidationStateChanged();
        };
    }

    private void ValidateEditModel()
    {
        ValidationMessageStore.Clear();

        AddRequiredMessageIfEmpty(nameof(InvoiceEditFormModel.VendorIdText), "VendorId");
        AddRequiredMessageIfEmpty(nameof(InvoiceEditFormModel.InvoiceNumber), "InvoiceNumber");
        AddRequiredMessageIfEmpty(nameof(InvoiceEditFormModel.Currency), "Currency");

        foreach (var item in EditModel.LineItems)
        {
            AddRequiredMessageIfEmpty(item, nameof(InvoiceLineItemEditFormModel.Description), "Description");

            if (item.Quantity < 0.0001m)
            {
                AddValidationMessage(
                    item,
                    nameof(InvoiceLineItemEditFormModel.Quantity),
                    "FieldMustBeGreaterThanZero",
                    "Quantity");
            }

            if (item.UnitPrice < 0)
            {
                AddValidationMessage(
                    item,
                    nameof(InvoiceLineItemEditFormModel.UnitPrice),
                    "FieldMustBeNonNegative",
                    "UnitPrice");
            }

            if (item.TaxRate < 0 || item.TaxRate > 100)
            {
                AddValidationMessage(
                    item,
                    nameof(InvoiceLineItemEditFormModel.TaxRate),
                    "FieldMustBeBetween",
                    "TaxRate",
                    "0",
                    "100");
            }
        }
    }

    private void AddRequiredMessageIfEmpty(string fieldName, string labelKey)
    {
        var value = fieldName switch
        {
            nameof(InvoiceEditFormModel.VendorIdText) => EditModel.VendorIdText,
            nameof(InvoiceEditFormModel.InvoiceNumber) => EditModel.InvoiceNumber,
            nameof(InvoiceEditFormModel.Currency) => EditModel.Currency,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(value))
        {
            AddValidationMessage(EditModel, fieldName, "RequiredFieldValidation", labelKey);
        }
    }

    private void AddRequiredMessageIfEmpty(InvoiceLineItemEditFormModel item, string fieldName, string labelKey)
    {
        if (fieldName == nameof(InvoiceLineItemEditFormModel.Description) &&
            string.IsNullOrWhiteSpace(item.Description))
        {
            AddValidationMessage(item, fieldName, "RequiredFieldValidation", labelKey);
        }
    }

    private void AddValidationMessage(object model, string fieldName, string messageKey, string labelKey, params object[] arguments)
    {
        var label = L[labelKey].Value;
        var formatArguments = new object[] { label }.Concat(arguments).ToArray();
        var message = string.Format(CultureInfo.CurrentCulture, L[messageKey].Value, formatArguments);

        ValidationMessageStore.Add(new FieldIdentifier(model, fieldName), message);
    }
}
