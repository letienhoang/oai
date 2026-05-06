using OAI.Infrastructure.DemoData;
using OAI.Infrastructure.Identity;

namespace OAI.Web.Endpoints;

public static class DemoDataEndpointExtensions
{
    public static IEndpointRouteBuilder MapDemoDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/dev/demo-data/seed", async (
            DemoDataSeeder demoDataSeeder,
            CancellationToken cancellationToken) =>
        {
            var result = await demoDataSeeder.SeedAsync(cancellationToken);

            return Results.Ok(result);
        })
        .RequireAuthorization(policy =>
        {
            policy.RequireRole(ApplicationRoles.Administrator);
        });

        endpoints.MapPost("/dev/demo-data/reset", async (
            DemoDataSeeder demoDataSeeder,
            CancellationToken cancellationToken) =>
        {
            var result = await demoDataSeeder.ResetAsync(cancellationToken);

            return Results.Ok(result);
        })
        .RequireAuthorization(policy =>
        {
            policy.RequireRole(ApplicationRoles.Administrator);
        });

        return endpoints;
    }
}
