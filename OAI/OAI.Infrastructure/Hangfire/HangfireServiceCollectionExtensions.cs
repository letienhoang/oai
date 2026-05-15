using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OAI.Infrastructure.Hangfire;

public static class HangfireServiceCollectionExtensions
{
    public static IServiceCollection AddOaiHangfireStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is required to configure Hangfire SQL Server storage.");
        }

        services.AddHangfire((serviceProvider, hangfireConfiguration) =>
        {
            hangfireConfiguration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseFilter(new AutomaticRetryAttribute
                {
                    Attempts = 3,
                    DelaysInSeconds = [30, 120, 300],
                    OnAttemptsExceeded = AttemptsExceededAction.Fail
                })
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                });
        });

        return services;
    }
}