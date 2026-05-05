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
        await SeedRolesAndPermissionsAsync();

        foreach (var userOptions in GetSeedUsers())
        {
            await SeedUserAsync(userOptions);
        }
    }

    private async Task SeedRolesAndPermissionsAsync()
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

            await SeedRolePermissionsAsync(role, roleName);
        }
    }

    private async Task SeedRolePermissionsAsync(IdentityRole<Guid> role, string roleName)
    {
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

    private IEnumerable<IdentitySeedUserOptions> GetSeedUsers()
    {
        if (_options.Users.Count > 0)
        {
            return _options.Users;
        }

        return
        [
            new()
            {
                Email = _options.AdminEmail,
                Password = _options.AdminPassword,
                DisplayName = _options.AdminDisplayName,
                Role = ApplicationRoles.Administrator
            }
        ];
    }

    private async Task SeedUserAsync(IdentitySeedUserOptions userOptions)
    {
        if (string.IsNullOrWhiteSpace(userOptions.Email))
        {
            throw new InvalidOperationException("Seed user email is required.");
        }

        if (string.IsNullOrWhiteSpace(userOptions.Password))
        {
            throw new InvalidOperationException($"Seed user '{userOptions.Email}' requires a password.");
        }

        if (string.IsNullOrWhiteSpace(userOptions.Role))
        {
            throw new InvalidOperationException($"Seed user '{userOptions.Email}' requires a role.");
        }

        if (!await _roleManager.RoleExistsAsync(userOptions.Role))
        {
            throw new InvalidOperationException($"Unable to find role '{userOptions.Role}' for seed user '{userOptions.Email}'.");
        }

        var user = await _userManager.FindByEmailAsync(userOptions.Email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userOptions.Email,
                Email = userOptions.Email,
                DisplayName = userOptions.DisplayName,
                EmailConfirmed = true,
                IsActive = userOptions.IsActive
            };

            var userResult = await _userManager.CreateAsync(user, userOptions.Password);
            ThrowIfFailed(userResult, $"Unable to create seed user '{userOptions.Email}'.");
        }
        else
        {
            user.DisplayName = userOptions.DisplayName;
            user.EmailConfirmed = true;
            user.IsActive = userOptions.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            ThrowIfFailed(updateResult, $"Unable to update seed user '{userOptions.Email}'.");
        }

        if (!await _userManager.IsInRoleAsync(user, userOptions.Role))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, userOptions.Role);
            ThrowIfFailed(addRoleResult, $"Unable to add seed user '{userOptions.Email}' to role '{userOptions.Role}'.");
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
