using OAI.Infrastructure.Identity;
using OAI.Infrastructure.SystemHealth;

namespace OAI.Web.Endpoints;

public static class SystemHealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapSystemHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dev/system-health", async (
            SystemHealthService systemHealthService,
            CancellationToken cancellationToken) =>
        {
            var result = await systemHealthService.CheckAsync(cancellationToken);

            return Results.Ok(result);
        })
        .RequireAuthorization(policy =>
        {
            policy.RequireRole(ApplicationRoles.Administrator);
        });

        return endpoints;
    }
}
