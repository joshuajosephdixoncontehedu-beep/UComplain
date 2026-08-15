using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

public record MobileReportListItemDto(
    Guid Id,
    string CaseReference,
    string CategoryName,
    DateTimeOffset CreatedAt,
    IncidentPriority Priority,
    VerificationStatus VerificationStatus,
    CaseStatus CaseStatus,
    int AttachmentCount,
    // ReportStatusProjection fields — see that class. Included on the list item, not
    // just the detail view, so the mobile app's report-list cards can show a badge and
    // progress bar without a second request per row.
    string StatusBadge,
    int? TrackerStage,
    int ProgressPercent,
    ReportListBucket Bucket);
