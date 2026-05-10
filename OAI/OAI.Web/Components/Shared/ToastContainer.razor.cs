using Microsoft.AspNetCore.Components;
using OAI.Web.Services;

namespace OAI.Web.Components.Shared;

public partial class ToastContainer
{
    protected override void OnInitialized()
    {
        ToastService.ToastsChanged += OnToastsChanged;
    }

    public void Dispose()
    {
        ToastService.ToastsChanged -= OnToastsChanged;
    }

    private void OnToastsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private static string GetToastClass(ToastType type)
    {
        return $"toast fade show oai-toast oai-toast-{GetTypeClass(type)}";
    }

    private static string GetIndicatorClass(ToastType type)
    {
        return $"oai-toast-indicator bg-{GetTypeClass(type)}";
    }

    private string GetTitle(ToastMessage toast)
    {
        if (!string.IsNullOrWhiteSpace(toast.Title))
        {
            return toast.Title;
        }

        return toast.Type switch
        {
            ToastType.Success => L["Success"],
            ToastType.Error => L["Error"],
            ToastType.Warning => L["Warning"],
            ToastType.Info => L["Info"],
            _ => L["Notification"]
        };
    }

    private static string GetRole(ToastType type)
    {
        return type is ToastType.Error or ToastType.Warning ? "alert" : "status";
    }

    private static string GetTypeClass(ToastType type)
    {
        return type switch
        {
            ToastType.Success => "success",
            ToastType.Error => "danger",
            ToastType.Warning => "warning",
            ToastType.Info => "info",
            _ => "secondary"
        };
    }
}
