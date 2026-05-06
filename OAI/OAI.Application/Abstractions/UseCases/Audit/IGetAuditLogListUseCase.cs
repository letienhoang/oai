using OAI.Application.Audit.Dtos;
using OAI.Application.Common;

namespace OAI.Application.Abstractions.UseCases.Audit;

public interface IGetAuditLogListUseCase
{
    Task<PagedResultDto<AuditLogListItemDto>> ExecuteAsync(
        GetAuditLogListRequestDto request,
        CancellationToken cancellationToken = default);
}