using Microsoft.AspNetCore.Localization;

namespace OAI.Web.Endpoints;

public static class LocalizationEndpointExtensions
{
    public static IEndpointRouteBuilder MapLocalizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/culture/set", (
            HttpContext httpContext,
            string culture,
            string? returnUrl) =>
        {
            if (!IsSupportedCulture(culture))
            {
                culture = "en";
            }

            var requestCulture = new RequestCulture(culture);

            httpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(requestCulture),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Lax
                });

            var redirectUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? "/"
                : returnUrl;

            if (!Uri.IsWellFormedUriString(redirectUrl, UriKind.Relative))
            {
                redirectUrl = "/";
            }

            return Results.LocalRedirect(redirectUrl);
        });

        return endpoints;
    }

    private static bool IsSupportedCulture(string culture)
    {
        return culture is "en" or "vi";
    }
}
