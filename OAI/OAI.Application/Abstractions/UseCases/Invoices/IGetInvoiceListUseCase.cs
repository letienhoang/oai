using OAI.Application.Common;
using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface IGetInvoiceListUseCase
{
    Task<PagedResultDto<InvoiceListItemDto>> ExecuteAsync(
        GetInvoiceListRequestDto request,
        CancellationToken cancellationToken = default);
}