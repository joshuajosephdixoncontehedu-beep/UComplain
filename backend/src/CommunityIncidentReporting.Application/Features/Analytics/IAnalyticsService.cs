using CommunityIncidentReporting.Application.Features.Analytics.Dtos;

namespace CommunityIncidentReporting.Application.Features.Analytics;

public interface IAnalyticsService
{
    Task<AnalyticsResponse> GetAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    /// <summary>One row per report created in range: reference, category, priority, statuses, timestamps.</summary>
    Task<byte[]> ExportCsvAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
}
