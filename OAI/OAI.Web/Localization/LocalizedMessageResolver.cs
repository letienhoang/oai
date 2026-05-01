using Microsoft.Extensions.Localization;

namespace OAI.Web.Localization;

public sealed class LocalizedMessageResolver
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizedMessageResolver(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string Resolve(
        string? messageCode,
        IReadOnlyDictionary<string, string>? parameters,
        string? fallbackMessage = null)
    {
        if (string.IsNullOrWhiteSpace(messageCode))
            return fallbackMessage ?? string.Empty;

        var localized = _localizer[messageCode];

        if (localized.ResourceNotFound)
            return fallbackMessage ?? messageCode;

        var value = localized.Value;

        if (parameters is null || parameters.Count == 0)
            return value;

        foreach (var parameter in parameters)
        {
            value = value.Replace(
                "{" + parameter.Key + "}",
                parameter.Value,
                StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }
}
