using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using OAI.Infrastructure.Identity;

namespace OAI.Web.Endpoints;

public static class AuthEndpointExtensions
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", async (
            [FromForm] LoginRequest request,
            string? returnUrl,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user is null || !user.IsActive)
            {
                return TypedResults.LocalRedirect(BuildInvalidLoginRedirect(returnUrl));
            }

            var signInResult = await signInManager.PasswordSignInAsync(
                user,
                request.Password,
                request.RememberMe,
                lockoutOnFailure: false);

            if (!signInResult.Succeeded)
            {
                return TypedResults.LocalRedirect(BuildInvalidLoginRedirect(returnUrl));
            }

            var redirectUrl = IsRelativeUrl(returnUrl) ? returnUrl! : "/";

            return TypedResults.LocalRedirect(redirectUrl);
        });

        endpoints.MapPost("/auth/logout", async (
            HttpContext httpContext,
            string? returnUrl) =>
        {
            await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            var redirectUrl = IsRelativeUrl(returnUrl) ? returnUrl! : "/login";

            return TypedResults.LocalRedirect(redirectUrl);
        });

        return endpoints;
    }

    private static string BuildInvalidLoginRedirect(string? returnUrl)
    {
        var query = new Dictionary<string, string?>
        {
            ["error"] = "invalid",
            ["returnUrl"] = IsRelativeUrl(returnUrl) ? returnUrl : null
        };

        return QueryHelpers.AddQueryString("/login", query);
    }

    private static bool IsRelativeUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Relative, out _)
            && url.StartsWith("/", StringComparison.Ordinal)
            && !url.StartsWith("//", StringComparison.Ordinal)
            && !url.StartsWith("/\\", StringComparison.Ordinal);
    }

    private sealed class LoginRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
