using OAI.Application.Common;
using OAI.Application.Invoices.Dtos;

namespace OAI.Application.Abstractions.UseCases.Invoices;

public interface IGetValidationIssueListUseCase
{
    Task<PagedResultDto<ValidationIssueListItemDto>> ExecuteAsync(
        GetValidationIssueListRequestDto request,
        CancellationToken cancellationToken = default);
}