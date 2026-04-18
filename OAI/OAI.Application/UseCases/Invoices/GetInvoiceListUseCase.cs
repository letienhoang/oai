using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Common;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Mappings;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class GetInvoiceListUseCase : IGetInvoiceListUseCase
{
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoiceListUseCase(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<PagedResultDto<InvoiceListItemDto>> ExecuteAsync(
        GetInvoiceListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PageNumber <= 0)
            throw new DomainException("PageNumber must be greater than zero.");

        if (request.PageSize <= 0)
            throw new DomainException("PageSize must be greater than zero.");

        var totalItems = await _invoiceRepository.CountAsync(request.Keyword, cancellationToken);

        var invoices = await _invoiceRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Keyword,
            cancellationToken);

        var items = invoices
            .Select(x => x.ToListItemDto())
            .ToList();

        return new PagedResultDto<InvoiceListItemDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = totalItems
        };
    }
}