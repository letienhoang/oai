using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using OAI.Application.Abstractions.UseCases.Vendors;
using OAI.Application.Vendors.Dtos;
using OAI.Infrastructure.Identity;
using OAI.Web.Components.Shared;
using OAI.Web.Localization;
using OAI.Web.Services;

namespace OAI.Web.Components.Pages.Vendors;

public partial class VendorManagement
{
    private const int DefaultPageSize = 10;

    [Inject]
    private IGetVendorListUseCase GetVendorListUseCase { get; set; } = default!;

    [Inject]
    private IUpsertVendorUseCase UpsertVendorUseCase { get; set; } = default!;

    [Inject]
    private CurrentUserAuthorizationService AuthorizationService { get; set; } = default!;

    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [Inject]
    private ILogger<VendorManagement> Logger { get; set; } = default!;

    private List<VendorListItemDto> Vendors { get; set; } = new();

    private VendorFormModel FormModel { get; set; } = new();

    private string? Keyword { get; set; }

    private int PageNumber { get; set; } = 1;

    private int PageSize { get; set; } = DefaultPageSize;

    private int TotalItems { get; set; }

    private int TotalPages { get; set; }

    private bool IsLoading { get; set; }

    private bool IsSaving { get; set; }

    private bool CanManageVendors { get; set; }

    private string? ErrorMessage { get; set; }

    private string? FormErrorMessage { get; set; }

    private string? SuccessMessage { get; set; }

    private ConfirmDialog? ConfirmDialog { get; set; }

    private bool CanGoPrevious => PageNumber > 1;

    private bool CanGoNext => PageNumber < TotalPages;

    private string FormTitle => FormModel.VendorId.HasValue
        ? L["EditVendor"]
        : L["CreateVendor"];

    protected override async Task OnInitializedAsync()
    {
        CanManageVendors = await AuthorizationService.IsAuthorizedAsync(ApplicationPolicies.ManageVendors);
        StartCreate();
        await LoadVendorsAsync();
    }

    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadVendorsAsync();
    }

    private async Task ClearFilterAsync()
    {
        Keyword = null;
        PageNumber = 1;
        await LoadVendorsAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        PageNumber--;
        await LoadVendorsAsync();
    }

    private async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        PageNumber++;
        await LoadVendorsAsync();
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    private void StartCreate()
    {
        FormModel = new VendorFormModel();
        FormErrorMessage = null;
        SuccessMessage = null;
    }

    private void StartEdit(VendorListItemDto vendor)
    {
        if (!CanManageVendors)
            return;

        FormModel = new VendorFormModel
        {
            VendorId = vendor.VendorId,
            Name = vendor.Name,
            TaxNumber = vendor.TaxNumber,
            Email = vendor.Email,
            Address = vendor.Address
        };

        FormErrorMessage = null;
        SuccessMessage = null;
    }

    private void ConfirmSaveVendor()
    {
        var isEdit = FormModel.VendorId.HasValue;

        ConfirmDialog?.Open(
            title: isEdit ? L["ConfirmUpdateVendorTitle"] : L["ConfirmCreateVendorTitle"],
            message: isEdit ? L["ConfirmUpdateVendorMessage"] : L["ConfirmCreateVendorMessage"],
            confirmText: L["Save"],
            cancelText: L["Cancel"],
            onConfirm: SaveVendorAsync,
            confirmButtonClass: "btn btn-primary");
    }

    private async Task SaveVendorAsync()
    {
        if (!await AuthorizationService.IsAuthorizedAsync(ApplicationPolicies.ManageVendors))
        {
            FormErrorMessage = L["ManageVendorsNotAllowed"];
            return;
        }

        FormErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(FormModel.Name))
        {
            FormErrorMessage = L["VendorNameRequired"];
            return;
        }

        var isEdit = FormModel.VendorId.HasValue;
        IsSaving = true;

        try
        {
            var savedVendor = await UpsertVendorUseCase.ExecuteAsync(
                new VendorUpsertRequestDto
                {
                    VendorId = FormModel.VendorId,
                    Name = FormModel.Name,
                    TaxNumber = FormModel.TaxNumber,
                    Email = FormModel.Email,
                    Address = FormModel.Address
                });

            SuccessMessage = isEdit
                ? L["VendorUpdatedSuccessfully"]
                : L["VendorCreatedSuccessfully"];

            FormModel = new VendorFormModel
            {
                VendorId = savedVendor.VendorId,
                Name = savedVendor.Name,
                TaxNumber = savedVendor.TaxNumber,
                Email = savedVendor.Email,
                Address = savedVendor.Address
            };

            await LoadVendorsAsync();
        }
        catch (Exception ex)
        {
            FormErrorMessage = L["VendorSaveFailed"];
            Logger.LogError(ex, "Failed to save vendor {VendorId}", FormModel.VendorId);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task LoadVendorsAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var result = await GetVendorListUseCase.ExecuteAsync(
                new GetVendorListRequestDto
                {
                    PageNumber = PageNumber,
                    PageSize = PageSize,
                    Filter = new VendorFilterDto
                    {
                        Keyword = Keyword
                    }
                });

            Vendors = result.Items.ToList();
            PageNumber = result.PageNumber;
            PageSize = result.PageSize;
            TotalItems = result.TotalItems;
            TotalPages = result.TotalPages;
        }
        catch (Exception ex)
        {
            Vendors.Clear();
            TotalItems = 0;
            TotalPages = 0;
            ErrorMessage = L["VendorListLoadFailed"];

            Logger.LogError(ex, "Failed to load vendor list.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string DisplayOrFallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? L["NotAvailable"] : value;
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    private sealed class VendorFormModel
    {
        public Guid? VendorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TaxNumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
    }
}
