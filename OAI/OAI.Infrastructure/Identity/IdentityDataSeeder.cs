using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace OAI.Infrastructure.Identity;

public sealed class IdentityDataSeeder
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentitySeedOptions _options;

    public IdentityDataSeeder(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<IdentitySeedOptions> options)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task SeedAsync()
    {
        foreach (var roleName in ApplicationRoles.All)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                ThrowIfFailed(roleResult, $"Unable to create role '{roleName}'.");
            }

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                throw new InvalidOperationException($"Unable to load role '{roleName}'.");
            }

            var expectedPermissions = ApplicationRolePermissions.GetPermissions(roleName);
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var existingPermissions = existingClaims
                .Where(claim => claim.Type == ApplicationClaimTypes.Permission)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var permission in expectedPermissions)
            {
                if (existingPermissions.Contains(permission))
                {
                    continue;
                }

                var claimResult = await _roleManager.AddClaimAsync(
                    role,
                    new Claim(ApplicationClaimTypes.Permission, permission));

                ThrowIfFailed(claimResult, $"Unable to add permission '{permission}' to role '{roleName}'.");
            }
        }

        var adminUser = await _userManager.FindByEmailAsync(_options.AdminEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = _options.AdminEmail,
                Email = _options.AdminEmail,
                DisplayName = _options.AdminDisplayName,
                EmailConfirmed = true,
                IsActive = true
            };

            var userResult = await _userManager.CreateAsync(adminUser, _options.AdminPassword);
            ThrowIfFailed(userResult, $"Unable to create admin user '{_options.AdminEmail}'.");
        }
        else
        {
            adminUser.DisplayName = _options.AdminDisplayName;
            adminUser.EmailConfirmed = true;
            adminUser.IsActive = true;

            var updateResult = await _userManager.UpdateAsync(adminUser);
            ThrowIfFailed(updateResult, $"Unable to update admin user '{_options.AdminEmail}'.");
        }

        if (!await _userManager.IsInRoleAsync(adminUser, ApplicationRoles.Administrator))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(adminUser, ApplicationRoles.Administrator);
            ThrowIfFailed(addRoleResult, $"Unable to add admin user '{_options.AdminEmail}' to Administrator role.");
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"{message} {errors}");
    }
}
