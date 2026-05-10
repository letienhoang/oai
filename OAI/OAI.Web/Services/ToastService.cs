namespace OAI.Web.Services;

public sealed class ToastService : IToastService, IDisposable
{
    private static readonly TimeSpan DefaultAutoDismissAfter = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource _disposeCancellation = new();
    private readonly object _syncRoot = new();
    private readonly List<ToastMessage> _toasts = [];

    public event Action? ToastsChanged;

    public IReadOnlyCollection<ToastMessage> Toasts
    {
        get
        {
            lock (_syncRoot)
            {
                return _toasts.ToArray();
            }
        }
    }

    public ToastMessage Show(
        ToastType type,
        string message,
        string? title = null,
        bool autoDismiss = true,
        TimeSpan? autoDismissAfter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var toast = new ToastMessage
        {
            Type = type,
            Message = message,
            Title = title,
            AutoDismiss = autoDismiss,
            AutoDismissAfter = autoDismissAfter ?? DefaultAutoDismissAfter
        };

        lock (_syncRoot)
        {
            _toasts.Add(toast);
        }

        NotifyToastsChanged();

        if (toast.AutoDismiss)
        {
            _ = AutoDismissAsync(toast.Id, toast.AutoDismissAfter, _disposeCancellation.Token);
        }

        return toast;
    }

    public ToastMessage Success(string message, string? title = null)
    {
        return Show(ToastType.Success, message, title);
    }

    public ToastMessage Error(string message, string? title = null)
    {
        return Show(ToastType.Error, message, title);
    }

    public ToastMessage Warning(string message, string? title = null)
    {
        return Show(ToastType.Warning, message, title);
    }

    public ToastMessage Info(string message, string? title = null)
    {
        return Show(ToastType.Info, message, title);
    }

    public void Remove(Guid toastId)
    {
        var removed = false;

        lock (_syncRoot)
        {
            var toast = _toasts.FirstOrDefault(item => item.Id == toastId);
            if (toast is not null)
            {
                removed = _toasts.Remove(toast);
            }
        }

        if (removed)
        {
            NotifyToastsChanged();
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            if (_toasts.Count == 0)
            {
                return;
            }

            _toasts.Clear();
        }

        NotifyToastsChanged();
    }

    public void Dispose()
    {
        _disposeCancellation.Cancel();
        _disposeCancellation.Dispose();
    }

    private async Task AutoDismissAsync(Guid toastId, TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            Remove(toastId);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void NotifyToastsChanged()
    {
        ToastsChanged?.Invoke();
    }
}
