using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Common;
using OAI.Application.Invoices.Dtos;
using OAI.Application.Mappings;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class GetInvoiceListUseCase : IGetInvoiceListUseCase
{
    private const int MaxPageSize = 100;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<GetInvoiceListUseCase> _logger;

    public GetInvoiceListUseCase(IInvoiceRepository invoiceRepository, ILogger<GetInvoiceListUseCase> logger)
    {
        _invoiceRepository = invoiceRepository;
        _logger = logger;
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

        var filter = request.Filter;

        if (filter.IssueDateFrom.HasValue &&
            filter.IssueDateTo.HasValue &&
            filter.IssueDateFrom.Value > filter.IssueDateTo.Value)
        {
            throw new DomainException("IssueDateFrom must be earlier than or equal to IssueDateTo.");
        }

        if (filter.TotalAmountFrom.HasValue &&
            filter.TotalAmountTo.HasValue &&
            filter.TotalAmountFrom.Value > filter.TotalAmountTo.Value)
        {
            throw new DomainException("TotalAmountFrom must be less than or equal to TotalAmountTo.");
        }

        var pageSize = Math.Min(request.PageSize, MaxPageSize);

        var totalItems = await _invoiceRepository.CountAsync(filter, cancellationToken);

        var invoices = await _invoiceRepository.GetPagedAsync(
            request.PageNumber,
            pageSize,
            filter,
            cancellationToken);

        var items = invoices
            .Select(x => x.ToListItemDto())
            .ToList();
        
        _logger.LogInformation(
            "Getting invoice list. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}",
            request.PageNumber,
            pageSize,
            filter.Keyword);

        return new PagedResultDto<InvoiceListItemDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}
