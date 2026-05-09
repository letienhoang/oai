using Microsoft.AspNetCore.Components;

namespace OAI.Web.Components.Shared;

public partial class ConfirmDialog
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool IsOpen { get; set; }

    private string Title { get; set; } = string.Empty;

    private string? Message { get; set; }

    private string ConfirmText { get; set; } = string.Empty;

    private string CancelText { get; set; } = string.Empty;

    private string ConfirmButtonClass { get; set; } = "btn btn-primary";

    private TaskCompletionSource<bool>? CompletionSource { get; set; }

    public Task<bool> ShowAsync(
        string title,
        string? message,
        string confirmText,
        string cancelText,
        string confirmButtonClass = "btn btn-primary")
    {
        CompletionSource?.TrySetResult(false);

        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        ConfirmButtonClass = confirmButtonClass;
        IsOpen = true;
        CompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        StateHasChanged();

        return CompletionSource.Task;
    }

    private Task CancelAsync()
    {
        Complete(false);

        return Task.CompletedTask;
    }

    private Task ConfirmAsync()
    {
        Complete(true);

        return Task.CompletedTask;
    }

    private void Complete(bool confirmed)
    {
        var completionSource = CompletionSource;
        CompletionSource = null;
        IsOpen = false;
        StateHasChanged();
        completionSource?.TrySetResult(confirmed);
    }
}
