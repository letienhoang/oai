namespace OAI.Infrastructure.Identity;

public sealed class IdentitySeedOptions
{
    public string AdminEmail { get; set; } = "admin@oai.local";

    public string AdminPassword { get; set; } = "Admin@123456";

    public string AdminDisplayName { get; set; } = "OAI Administrator";

    public List<IdentitySeedUserOptions> Users { get; set; } =
    [
        new()
        {
            Email = "admin@oai.local",
            Password = "Admin@123456",
            DisplayName = "OAI Administrator",
            Role = ApplicationRoles.Administrator
        },
        new()
        {
            Email = "accountant@oai.local",
            Password = "Accountant@123456",
            DisplayName = "OAI Accountant",
            Role = ApplicationRoles.Accountant
        },
        new()
        {
            Email = "auditor@oai.local",
            Password = "Auditor@123456",
            DisplayName = "OAI Auditor",
            Role = ApplicationRoles.Auditor
        },
        new()
        {
            Email = "viewer@oai.local",
            Password = "Viewer@123456",
            DisplayName = "OAI Viewer",
            Role = ApplicationRoles.Viewer
        }
    ];
}

public sealed class IdentitySeedUserOptions
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
