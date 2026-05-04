namespace OAI.Infrastructure.Identity;

public sealed class IdentitySeedOptions
{
    public string AdminEmail { get; set; } = "admin@oai.local";

    public string AdminPassword { get; set; } = "Admin@123456";

    public string AdminDisplayName { get; set; } = "OAI Administrator";
}
