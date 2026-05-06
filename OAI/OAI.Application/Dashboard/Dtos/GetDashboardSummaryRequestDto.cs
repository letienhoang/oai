namespace OAI.Application.Dashboard.Dtos;

public sealed record GetDashboardSummaryRequestDto
{
    public DashboardFilterDto Filter { get; init; } = new();
}
