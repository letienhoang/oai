using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface IUpdateInvoiceUseCase
{
    Task<InvoiceDetailDto> ExecuteAsync(
        InvoiceUpdateRequestDto request,
        CancellationToken cancellationToken = default);
}