using OAI.Application.Abstractions.Persistence;
using OAI.Application.Abstractions.UseCases.Audit;
using OAI.Application.Audit.Dtos;

namespace OAI.Application.UseCases.Audit;

public sealed class GetAuditLogFilterOptionsUseCase : IGetAuditLogFilterOptionsUseCase
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogFilterOptionsUseCase(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<AuditLogFilterOptionsDto> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var entityNames = await _auditLogRepository.GetEntityNameOptionsAsync(cancellationToken);
        var actionTypes = await _auditLogRepository.GetActionTypeOptionsAsync(cancellationToken);
        var sources = await _auditLogRepository.GetSourceOptionsAsync(cancellationToken);

        return new AuditLogFilterOptionsDto
        {
            EntityNames = entityNames,
            ActionTypes = actionTypes,
            Sources = sources
        };
    }
}
