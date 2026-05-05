using OAI.Application.Audit.Dtos;

namespace OAI.Application.Abstractions.UseCases.Audit;

public interface IGetAuditLogFilterOptionsUseCase
{
    Task<AuditLogFilterOptionsDto> ExecuteAsync(
        CancellationToken cancellationToken = default);
}
