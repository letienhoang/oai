using OAI.Application.Dashboard.Dtos;

namespace OAI.Application.Abstractions.UseCases.Dashboard;

public interface IGetDashboardSummaryUseCase
{
    Task<DashboardSummaryDto> ExecuteAsync(
        GetDashboardSummaryRequestDto request,
        CancellationToken cancellationToken = default);

    Task<DashboardSummaryDto> ExecuteAsync(
        CancellationToken cancellationToken = default);
}
