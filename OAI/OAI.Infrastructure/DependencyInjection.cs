using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAI.Application.Abstractions.Persistence;
using OAI.Infrastructure.Persistence;
using OAI.Infrastructure.Repositories;

namespace OAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OaiDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}