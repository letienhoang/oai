namespace OAI.Infrastructure.Identity;

public static class ApplicationRoles
{
    public const string Administrator = nameof(Administrator);
    public const string Accountant = nameof(Accountant);
    public const string Auditor = nameof(Auditor);
    public const string Viewer = nameof(Viewer);

    public static readonly string[] All =
    [
        Administrator,
        Accountant,
        Auditor,
        Viewer
    ];
}
