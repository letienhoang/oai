using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OAI.Infrastructure.DemoData;
using OAI.Infrastructure.Identity;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Persistence;

namespace OAI.Infrastructure.SystemHealth;

public sealed class SystemHealthService
{
    private readonly OaiDbContext _dbContext;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly DemoDataSeedOptions _demoDataSeedOptions;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly OcrOptions _ocrOptions;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public SystemHealthService(
        OaiDbContext dbContext,
        IHostEnvironment hostEnvironment,
        IOptions<DemoDataSeedOptions> demoDataSeedOptions,
        IOptions<FileStorageOptions> fileStorageOptions,
        IOptions<OcrOptions> ocrOptions,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _hostEnvironment = hostEnvironment;
        _demoDataSeedOptions = demoDataSeedOptions.Value;
        _fileStorageOptions = fileStorageOptions.Value;
        _ocrOptions = ocrOptions.Value;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public async Task<SystemHealthCheckResult> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        var canConnectToDatabase = await _dbContext.Database.CanConnectAsync(cancellationToken);
        var pendingMigrations = Array.Empty<string>();

        var vendorCount = 0;
        var invoiceCount = 0;
        var demoInvoiceCount = 0;
        var auditLogCount = 0;
        var identityUserCount = 0;
        var identityRoleCount = 0;

        if (canConnectToDatabase)
        {
            pendingMigrations = (await _dbContext.Database
                    .GetPendingMigrationsAsync(cancellationToken))
                .ToArray();

            vendorCount = await _dbContext.Vendors.CountAsync(cancellationToken);
            invoiceCount = await _dbContext.Invoices.CountAsync(cancellationToken);
            demoInvoiceCount = await _dbContext.Invoices
                .CountAsync(
                    invoice => invoice.InvoiceNumber.StartsWith(_demoDataSeedOptions.InvoiceNumberPrefix),
                    cancellationToken);
            auditLogCount = await _dbContext.AuditLogs.CountAsync(cancellationToken);

            var userManager = _serviceProvider.GetService<UserManager<ApplicationUser>>();
            var roleManager = _serviceProvider.GetService<RoleManager<IdentityRole<Guid>>>();

            if (userManager is not null)
            {
                identityUserCount = await userManager.Users.CountAsync(cancellationToken);
            }

            if (roleManager is not null)
            {
                identityRoleCount = await roleManager.Roles.CountAsync(cancellationToken);
            }
        }

        var llmSection = _configuration.GetSection("Llm");

        return new SystemHealthCheckResult
        {
            EnvironmentName = _hostEnvironment.EnvironmentName,
            MachineName = Environment.MachineName,
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            CheckedAt = DateTimeOffset.UtcNow,
            CanConnectToDatabase = canConnectToDatabase,
            PendingMigrationCount = pendingMigrations.Length,
            PendingMigrations = pendingMigrations,
            VendorCount = vendorCount,
            InvoiceCount = invoiceCount,
            DemoInvoiceCount = demoInvoiceCount,
            AuditLogCount = auditLogCount,
            IdentityUserCount = identityUserCount,
            IdentityRoleCount = identityRoleCount,
            DemoDataSeedEnabled = _demoDataSeedOptions.Enabled,
            DemoInvoiceNumberPrefix = _demoDataSeedOptions.InvoiceNumberPrefix,
            FileStorageConfigured = !string.IsNullOrWhiteSpace(_fileStorageOptions.RootPath),
            FileStorageRootPath = _fileStorageOptions.RootPath,
            OcrConfigured = !string.IsNullOrWhiteSpace(_ocrOptions.TessDataPath) &&
                !string.IsNullOrWhiteSpace(_ocrOptions.Languages),
            OcrLanguages = _ocrOptions.Languages,
            LlmEnabled = llmSection.GetValue<bool>("Enabled"),
            LlmProvider = llmSection.GetValue<string>("Provider") ?? string.Empty,
            LlmModel = llmSection.GetValue<string>("Model") ?? string.Empty
        };
    }
}
