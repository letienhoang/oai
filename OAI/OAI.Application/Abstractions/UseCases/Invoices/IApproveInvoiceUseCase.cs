using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface IApproveInvoiceUseCase
{
    Task<ApproveInvoiceResultDto> ExecuteAsync(
        ApproveInvoiceRequestDto request,
        CancellationToken cancellationToken = default);
}