using CommunityIncidentReporting.Application.Features.Dashboard.Dtos;

namespace CommunityIncidentReporting.Application.Features.Dashboard;

public interface IDashboardService
{
    /// <summary>Defaults to the last 30 days (inclusive of today) when from/to are omitted.</summary>
    Task<DashboardResponse> GetAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
}
