using CommunityIncidentReporting.Application.Features.Dashboard.Dtos;

namespace CommunityIncidentReporting.Application.Features.Analytics.Dtos;

public record AnalyticsResponse(
    DateOnly From,
    DateOnly To,
    DashboardMetricsDto Metrics,
    IReadOnlyList<TimeSeriesPointDto> ReportVolumeOverTime,
    IReadOnlyList<NamedCountDto> CategoryDistribution,
    IReadOnlyList<NamedCountDto> StatusDistribution,
    IReadOnlyList<NamedCountDto> VerificationOutcomeDistribution,
    IReadOnlyList<AssignmentWorkloadDto> AssignmentWorkload,
    IReadOnlyList<CategoryResponseTimeDto> ResolutionTimeByCategory,
    // Additive — see docs/mobile-client-backend-extension.md's admin-unification section.
    IReadOnlyList<NamedCountDto> ResolvedBySourceChannel,
    IReadOnlyList<SourceChannelVerificationOutcomeDto> VerificationOutcomesBySourceChannel);
