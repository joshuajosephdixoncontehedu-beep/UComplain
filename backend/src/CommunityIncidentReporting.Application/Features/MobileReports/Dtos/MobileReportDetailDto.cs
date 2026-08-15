using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// Reporter-safe report detail — deliberately omits InternalNotes, the AuditLog trail,
/// AssignedAdmin identity, and StatusHistory.Notes/actor. See IMobileReportService.
/// </summary>
public record MobileReportDetailDto(
    Guid Id,
    string CaseReference,
    Guid CategoryId,
    string CategoryName,
    SourceChannel SourceChannel,
    string Description,
    DateTimeOffset IncidentOccurredAt,
    string LocationDescription,
    double? Latitude,
    double? Longitude,
    string? Landmark,
    VerificationStatus VerificationStatus,
    CaseStatus CaseStatus,
    IncidentPriority Priority,
    string? ResolutionSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? WithdrawnAt,
    string? WithdrawalReason,
    IReadOnlyList<MobileReportStatusHistoryDto> StatusHistory,
    IReadOnlyList<MediaAttachmentDto> Attachments);
