using Microsoft.Extensions.DependencyInjection;
using OAI.Application.Abstractions.Services;
using OAI.Application.Abstractions.UseCases.Dashboard;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Services;
using OAI.Application.UseCases.Dashboard;
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
        services.AddScoped<IUpdateInvoiceUseCase, UpdateInvoiceUseCase>();
        services.AddScoped<IGetDashboardSummaryUseCase, GetDashboardSummaryUseCase>();
        services.AddScoped<ICompareInvoiceExtractionUseCase, CompareInvoiceExtractionUseCase>();
        services.AddScoped<IApproveInvoiceUseCase, ApproveInvoiceUseCase>();
        services.AddScoped<IRejectInvoiceUseCase, RejectInvoiceUseCase>();
        services.AddScoped<IMoveInvoiceToPendingReviewUseCase, MoveInvoiceToPendingReviewUseCase>();

        services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();

        return services;
    }
}