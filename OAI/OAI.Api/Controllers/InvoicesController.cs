using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAI.Application.Abstractions.UseCases.Invoices;
using OAI.Application.Common;
using OAI.Application.Invoices.Dtos;
using OAI.Domain.Enums;
using OAI.Infrastructure.Identity;

namespace OAI.Api.Controllers;

[Authorize(Policy = ApplicationPolicies.ViewInvoices)]
[ApiController]
[Route("api/invoices")]
[Produces("application/json")]
public sealed class InvoicesController : ControllerBase
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly IGetInvoiceListUseCase _getInvoiceListUseCase;
    private readonly IGetInvoiceDetailUseCase _getInvoiceDetailUseCase;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IGetInvoiceListUseCase getInvoiceListUseCase,
        IGetInvoiceDetailUseCase getInvoiceDetailUseCase,
        ILogger<InvoicesController> logger)
    {
        _getInvoiceListUseCase = getInvoiceListUseCase;
        _getInvoiceDetailUseCase = getInvoiceDetailUseCase;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<InvoiceListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<InvoiceListItemDto>>> GetInvoices(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] string? keyword,
        [FromQuery] string? status,
        [FromQuery] Guid? vendorId,
        [FromQuery] DateOnly? issueDateFrom,
        [FromQuery] DateOnly? issueDateTo,
        CancellationToken cancellationToken)
    {
        var effectivePageNumber = pageNumber ?? DefaultPageNumber;
        var effectivePageSize = pageSize ?? DefaultPageSize;

        if (effectivePageNumber <= 0)
        {
            return BadRequest(new
            {
                message = "pageNumber must be greater than 0."
            });
        }

        if (effectivePageSize <= 0)
        {
            return BadRequest(new
            {
                message = "pageSize must be greater than 0."
            });
        }

        if (effectivePageSize > MaxPageSize)
        {
            return BadRequest(new
            {
                message = $"pageSize must be less than or equal to {MaxPageSize}."
            });
        }

        if (issueDateFrom.HasValue &&
            issueDateTo.HasValue &&
            issueDateFrom.Value > issueDateTo.Value)
        {
            return BadRequest(new
            {
                message = "issueDateFrom must be less than or equal to issueDateTo."
            });
        }

        InvoiceStatus? parsedStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var statusValue))
            {
                return BadRequest(new
                {
                    message = $"Invalid invoice status '{status}'."
                });
            }

            parsedStatus = statusValue;
        }

        var request = new GetInvoiceListRequestDto
        {
            PageNumber = effectivePageNumber,
            PageSize = effectivePageSize,
            Filter = new InvoiceListFilterDto
            {
                Keyword = keyword,
                Status = parsedStatus,
                VendorId = vendorId,
                IssueDateFrom = issueDateFrom,
                IssueDateTo = issueDateTo
            }
        };

        var result = await _getInvoiceListUseCase.ExecuteAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDetailDto>> GetInvoiceById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getInvoiceDetailUseCase.ExecuteAsync(
                new GetInvoiceDetailRequestDto
                {
                    InvoiceId = id
                },
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex) when (IsNotFoundException(ex))
        {
            _logger.LogWarning(
                ex,
                "Invoice detail was not found. InvoiceId: {InvoiceId}",
                id);

            return NotFound(new
            {
                message = "Invoice was not found.",
                invoiceId = id
            });
        }
    }

    private static bool IsNotFoundException(Exception exception)
    {
        // Replace this with the actual not-found exception type used by OAI.Application
        // if the solution already defines one, for example:
        // return exception is EntityNotFoundException or NotFoundException;

        return exception.GetType().Name.Contains(
            "NotFound",
            StringComparison.OrdinalIgnoreCase);
    }
}