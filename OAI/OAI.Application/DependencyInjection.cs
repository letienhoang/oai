using Microsoft.Extensions.DependencyInjection;
using OAI.Application.Abstractions.Services;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Services;
using OAI.Application.UseCases.Invoices;

namespace OAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateInvoiceUseCase, CreateInvoiceUseCase>();
        services.AddScoped<IValidateInvoiceUseCase, ValidateInvoiceUseCase>();
        services.AddScoped<IGetInvoiceListUseCase, GetInvoiceListUseCase>();
        services.AddScoped<IGetInvoiceDetailUseCase, GetInvoiceDetailUseCase>();
        services.AddScoped<IGetValidationIssueListUseCase, GetValidationIssueListUseCase>();

        services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();

        return services;
    }
}