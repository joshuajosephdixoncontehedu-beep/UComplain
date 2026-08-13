using CommunityIncidentReporting.Application.Features.Reports.Dtos;

namespace CommunityIncidentReporting.Application.Features.Dashboard.Dtos;

public record DashboardResponse(
    DateOnly From,
    DateOnly To,
    DashboardMetricsDto Current,
    DashboardMetricsDto Previous,
    IReadOnlyList<TimeSeriesPointDto> ReportVolumeOverTime,
    IReadOnlyList<NamedCountDto> CategoryDistribution,
    IReadOnlyList<NamedCountDto> StatusDistribution,
    IReadOnlyList<NamedCountDto> VerificationOutcomeDistribution,
    IReadOnlyList<NamedCountDto> TopHotspots,
    IReadOnlyList<PriorityReportItemDto> PriorityReports,
    IReadOnlyList<AuditLogEntryDto> RecentActivity,
    VerificationQueueSnapshotDto VerificationQueueSnapshot);
