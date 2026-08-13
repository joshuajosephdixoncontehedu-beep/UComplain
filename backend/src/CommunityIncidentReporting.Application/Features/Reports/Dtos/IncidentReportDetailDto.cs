using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record IncidentReportDetailDto(
    Guid Id,
    string CaseReference,
    Guid ReporterId,
    string ReporterMaskedContact,
    bool ReporterIsRestricted,
    Guid CategoryId,
    string CategoryName,
    SourceChannel SourceChannel,
    string Description,
    DateTimeOffset IncidentOccurredAt,
    string LocationDescription,
    double? Latitude,
    double? Longitude,
    string? MediaReference,
    VerificationStatus VerificationStatus,
    CaseStatus CaseStatus,
    IncidentPriority Priority,
    Guid? AssignedAdminId,
    string? AssignedAdminName,
    string? ResolutionSummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<VerificationEventDto> VerificationHistory,
    IReadOnlyList<StatusHistoryDto> StatusHistory,
    IReadOnlyList<InternalNoteDto> Notes,
    IReadOnlyList<ReportAssignmentDto> Assignments,
    IReadOnlyList<AuditLogEntryDto> AuditTrail);
