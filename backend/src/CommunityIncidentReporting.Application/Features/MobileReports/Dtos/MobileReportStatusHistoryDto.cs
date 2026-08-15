using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// Deliberately excludes StatusHistory.Notes and ChangedByAdmin — those may contain
/// internal admin commentary not meant for the reporter. See IMobileReportService.
/// </summary>
public record MobileReportStatusHistoryDto(CaseStatus PreviousStatus, CaseStatus NewStatus, DateTimeOffset CreatedAt);
