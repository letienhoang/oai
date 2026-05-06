using Microsoft.AspNetCore.WebUtilities;

namespace OAI.Web.Components.Layout;

public partial class AuthMenu
{
    private string LoginUrl
    {
        get
        {
            var currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            var returnUrl = string.IsNullOrWhiteSpace(currentUri)
                ? "/"
                : $"/{currentUri}";

            return QueryHelpers.AddQueryString(
                "/login",
                "returnUrl",
                returnUrl);
        }
    }

    private static string UserDisplayName(System.Security.Claims.ClaimsPrincipal user)
    {
        return user.Identity?.Name ?? string.Empty;
    }
}