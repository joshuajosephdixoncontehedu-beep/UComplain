namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// The reporter-facing presentation of a report's status — badge text, which of the
/// five tracker stages (1-5) is reached (null = not on the tracker at all, e.g.
/// Rejected/Duplicate/Withdrawn), a progress percentage for list-card progress bars, and
/// which list tab it belongs in. Always computed by ReportStatusProjection, never stored.
/// </summary>
public record ReportStatusProjectionDto(string BadgeLabel, int? TrackerStage, int ProgressPercent, ReportListBucket Bucket);
