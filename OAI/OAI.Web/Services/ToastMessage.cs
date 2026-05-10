namespace OAI.Web.Services;

public sealed record ToastMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public ToastType Type { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? Title { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public bool AutoDismiss { get; init; } = true;

    public TimeSpan AutoDismissAfter { get; init; } = TimeSpan.FromSeconds(5);
}
