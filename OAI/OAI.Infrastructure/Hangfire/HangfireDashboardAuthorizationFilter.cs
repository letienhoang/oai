using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OAI.Infrastructure.Identity;

namespace OAI.Infrastructure.Hangfire;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var authorizationService =
            httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

        var authorizationResult = authorizationService
            .AuthorizeAsync(httpContext.User, ApplicationPolicies.ManageBackgroundJobs)
            .GetAwaiter()
            .GetResult();

        return authorizationResult.Succeeded;
    }
}