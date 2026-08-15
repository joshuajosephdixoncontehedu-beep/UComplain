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
    int AttachmentCount);
