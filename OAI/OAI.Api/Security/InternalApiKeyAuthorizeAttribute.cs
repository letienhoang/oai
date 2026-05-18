using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using OAI.Api.Options;

namespace OAI.Api.Security;

public sealed class InternalApiKeyAuthorizeAttribute : TypeFilterAttribute
{
    public const string HeaderName = "X-OAI-Internal-Api-Key";

    public InternalApiKeyAuthorizeAttribute()
        : base(typeof(InternalApiKeyAuthorizationFilter))
    {
    }

    private sealed class InternalApiKeyAuthorizationFilter : IAuthorizationFilter
    {
        private readonly InternalApiOptions _options;

        public InternalApiKeyAuthorizationFilter(IOptions<InternalApiOptions> options)
        {
            _options = options.Value;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // This key is only for trusted OAI.Web -> OAI.Api server-side calls.
            // Change it in production and never expose it to browser JavaScript.
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var values) ||
                values.Count != 1 ||
                !string.Equals(values[0], _options.ApiKey, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
