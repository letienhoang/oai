using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using OAI.Web.Localization;

namespace OAI.Web.Components.Pages.Auth;

public partial class Login
{
    [Inject]
    private IStringLocalizer<SharedResource> L { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }

    protected string LoginActionUrl
    {
        get
        {
            var returnUrl = IsRelativeUrl(ReturnUrl) ? ReturnUrl! : "/";

            return QueryHelpers.AddQueryString(
                "/auth/login",
                "returnUrl",
                returnUrl);
        }
    }

    protected string? ErrorMessage => string.Equals(Error, "invalid", StringComparison.OrdinalIgnoreCase)
        ? L["InvalidLoginAttempt"].Value
        : null;

    private static bool IsRelativeUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Relative, out _)
            && url.StartsWith("/", StringComparison.Ordinal)
            && !url.StartsWith("//", StringComparison.Ordinal)
            && !url.StartsWith("/\\", StringComparison.Ordinal);
    }
}
