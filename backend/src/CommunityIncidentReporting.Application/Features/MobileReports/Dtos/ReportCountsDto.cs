namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// Counts per ReportStatusProjection bucket for the caller's own reports — backs the
/// mobile app's list-tab badges. Total counts every one of the caller's reports,
/// including any in the NotListed bucket (withdrawn reports), so Active + Resolved +
/// Rejected does not always sum to Total.
/// </summary>
public record ReportCountsDto(int Active, int Resolved, int Rejected, int Total);
