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

        _logger.LogInformation(
            "Getting audit log list. PageNumber: {PageNumber}, PageSize: {PageSize}, Keyword: {Keyword}, EntityName: {EntityName}, ActionType: {ActionType}",
            request.PageNumber,
            request.PageSize,
            request.Keyword,
            request.EntityName,
            request.ActionType);

        var totalItems = await _auditLogRepository.CountAsync(
            request.Keyword,
            request.EntityName,
            request.ActionType,
            cancellationToken);

        var logs = await _auditLogRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Keyword,
            request.EntityName,
            request.ActionType,
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
            PageSize = request.PageSize,
            TotalItems = totalItems
        };
    }
}