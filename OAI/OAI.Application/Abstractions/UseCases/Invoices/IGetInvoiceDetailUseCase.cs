using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface IGetInvoiceDetailUseCase
{
    Task<InvoiceDetailDto> ExecuteAsync(
        GetInvoiceDetailRequestDto request,
        CancellationToken cancellationToken = default);
}