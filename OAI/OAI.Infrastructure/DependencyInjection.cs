using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OAI.Application.Abstractions.BackgroundJobs;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Infrastructure.Audit;
using OAI.Infrastructure.BackgroundJobs;
using OAI.Infrastructure.DemoData;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Persistence;
using OAI.Infrastructure.Repositories;
using OAI.Infrastructure.Services;
using OAI.Infrastructure.Services.Llm;
using OAI.Infrastructure.SystemHealth;

namespace OAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetRequiredSection("FileStorage"));
        
        services.AddOptions<OcrOptions>()
            .Bind(configuration.GetRequiredSection("Ocr"));
        
        services.AddOptions<LlmOptions>()
            .Bind(configuration.GetRequiredSection("Llm"));

        services.Configure<DemoDataSeedOptions>(
            configuration.GetSection("DemoDataSeed"));
        
        services.TryAddScoped<ICurrentUserContext, SystemCurrentUserContext>();
        services.AddScoped<AuditTrailInterceptor>();
        
        services.AddDbContext<OaiDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(sp.GetRequiredService<AuditTrailInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IValidationIssueRepository, ValidationIssueRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUploadBatchRepository, UploadBatchRepository>();
        services.AddScoped<IUploadBatchFileRepository, UploadBatchFileRepository>();
        
        services.AddScoped<RuleBasedInvoiceTextParser>();
        services.AddScoped<OpenAiInvoiceTextParser>();
        services.AddScoped<IInvoiceTextParser, HybridInvoiceTextParser>();
        
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IOcrService, TesseractOcrService>();
        
        services.AddScoped<IInvoiceExtractionService, InvoiceExtractionService>();
        services.AddScoped<IInvoiceExtractionComparisonService, InvoiceExtractionComparisonService>();
        
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<SystemHealthService>();
        
        services.AddScoped<IBackgroundJobClient, HangfireBackgroundJobClient>();
        
        return services;
    }
}
