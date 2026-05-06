using Microsoft.AspNetCore.Components;

namespace OAI.Web.Components.Shared;

public partial class ConfirmDialog
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool IsOpen { get; set; }

    private bool IsRunning { get; set; }

    private string Title { get; set; } = string.Empty;

    private string? Message { get; set; }

    private string ConfirmText { get; set; } = string.Empty;

    private string CancelText { get; set; } = string.Empty;

    private string ConfirmButtonClass { get; set; } = "btn btn-primary";

    private Func<Task>? OnConfirm { get; set; }

    public void Open(
        string title,
        string? message,
        string confirmText,
        string cancelText,
        Func<Task> onConfirm,
        string confirmButtonClass = "btn btn-primary")
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        OnConfirm = onConfirm;
        ConfirmButtonClass = confirmButtonClass;
        IsRunning = false;
        IsOpen = true;
        StateHasChanged();
    }

    public void Close()
    {
        if (IsRunning)
            return;

        IsOpen = false;
        OnConfirm = null;
        StateHasChanged();
    }

    private async Task ConfirmAsync()
    {
        if (OnConfirm is null || IsRunning)
            return;

        IsRunning = true;

        try
        {
            await OnConfirm();
            IsOpen = false;
            OnConfirm = null;
        }
        finally
        {
            IsRunning = false;
        }
    }
}