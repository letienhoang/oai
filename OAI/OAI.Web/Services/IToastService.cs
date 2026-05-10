namespace OAI.Web.Services;

public interface IToastService
{
    event Action? ToastsChanged;

    IReadOnlyCollection<ToastMessage> Toasts { get; }

    ToastMessage Show(
        ToastType type,
        string message,
        string? title = null,
        bool autoDismiss = true,
        TimeSpan? autoDismissAfter = null);

    ToastMessage Success(string message, string? title = null);

    ToastMessage Error(string message, string? title = null);

    ToastMessage Warning(string message, string? title = null);

    ToastMessage Info(string message, string? title = null);

    void Remove(Guid toastId);

    void Clear();
}
