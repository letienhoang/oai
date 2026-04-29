using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface IRejectInvoiceUseCase
{
    Task<RejectInvoiceResultDto> ExecuteAsync(
        RejectInvoiceRequestDto request,
        CancellationToken cancellationToken = default);
}