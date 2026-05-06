using Microsoft.Extensions.Logging;
using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Audit;
using OAI.Application.Audit.Dtos;
using OAI.Application.Common;
using OAI.Domain.Exceptions;

namespace OAI.Application.UseCases.Invoices;

public sealed class GetAuditLogListUseCase : IGetAuditLogListUseCase
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<GetAuditLogListUseCase> _logger;

    public GetAuditLogListUseCase(
        IAuditLogRepository auditLogRepository,
        ILogger<GetAuditLogListUseCase> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<PagedResultDto<AuditLogListItemDto>> ExecuteAsync(
        GetAuditLogListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PageNumber <= 0)
            throw new DomainException("PageNumber must be greater than zero.");

        if (request.PageSize <= 0)
            throw new DomainException("PageSize must be greater than zero.");

        if (request.Filter.OccurredAtFrom.HasValue
            && request.Filter.OccurredAtTo.HasValue
            && request.Filter.OccurredAtFrom.Value > request.Filter.OccurredAtTo.Value)
        {
            throw new DomainException("OccurredAtFrom must be earlier than or equal to OccurredAtTo.");
        }

        var pageSize = Math.Min(request.PageSize, 100);

        _logger.LogInformation(
            "Getting audit log list. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}, EntityName: {EntityName}, ActionType: {ActionType}, UserName: {UserName}, Source: {Source}, OccurredAtFrom: {OccurredAtFrom}, OccurredAtTo: {OccurredAtTo}",
            request.PageNumber,
            pageSize,
            request.Filter.Keyword,
            request.Filter.EntityName,
            request.Filter.ActionType,
            request.Filter.UserName,
            request.Filter.Source,
            request.Filter.OccurredAtFrom,
            request.Filter.OccurredAtTo);

        var totalItems = await _auditLogRepository.CountAsync(
            request.Filter,
            cancellationToken);

        var logs = await _auditLogRepository.GetPagedAsync(
            request.PageNumber,
            pageSize,
            request.Filter,
            cancellationToken);

        var items = logs
            .Select(x => new AuditLogListItemDto
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                ActionType = x.ActionType.ToString(),
                UserId = x.UserId,
                UserName = x.UserName,
                CorrelationId = x.CorrelationId,
                OccurredAt = x.OccurredAt,
                Source = x.Source,
                OldValuesJson = x.OldValuesJson,
                NewValuesJson = x.NewValuesJson
            })
            .ToList();

        return new PagedResultDto<AuditLogListItemDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}
