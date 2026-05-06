using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface ICreateInvoiceUseCase
{
    Task<InvoiceDetailDto> ExecuteAsync(
        InvoiceCreateRequestDto request,
        CancellationToken cancellationToken = default);
}