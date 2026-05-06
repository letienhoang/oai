using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

namespace OAI.Web.Components.Layout;

public partial class LanguageSelector
{
    private const string EnglishCulture = "en";
    private const string VietnameseCulture = "vi";

    private string CurrentCultureName => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    private string CurrentCultureLabel => CurrentCultureName switch
    {
        VietnameseCulture => "VI",
        _ => "EN"
    };

    private bool IsCurrentCulture(string culture)
    {
        return string.Equals(CurrentCultureName, culture, StringComparison.OrdinalIgnoreCase);
    }

    private void ChangeCulture(string culture)
    {
        if (IsCurrentCulture(culture))
        {
            return;
        }

        var currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        var returnUrl = string.IsNullOrWhiteSpace(currentUri)
            ? "/"
            : $"/{currentUri}";

        var url = QueryHelpers.AddQueryString(
            "/culture/set",
            new Dictionary<string, string?>
            {
                ["culture"] = culture,
                ["returnUrl"] = returnUrl
            });

        NavigationManager.NavigateTo(url, forceLoad: true);
    }
}