using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.Services;
using OAI.Infrastructure.Audit;
using OAI.Infrastructure.Options;
using OAI.Infrastructure.Persistence;
using OAI.Infrastructure.Repositories;
using OAI.Infrastructure.Services;

namespace OAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
        
        services.AddOptions<OcrOptions>()
            .Bind(configuration.GetRequiredSection("Ocr"));
        
        services.AddScoped<AuditTrailInterceptor>();
        
        services.AddDbContext<OaiDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(sp.GetRequiredService<AuditTrailInterceptor>());
        });

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IOcrService, TesseractOcrService>();
        services.AddScoped<IInvoiceExtractionService, InvoiceExtractionService>();

        return services;
    }
}