using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record IncidentReportListItemDto(
    Guid Id,
    string CaseReference,
    string CategoryName,
    string LocationDescription,
    DateTimeOffset CreatedAt,
    IncidentPriority Priority,
    VerificationStatus VerificationStatus,
    CaseStatus CaseStatus,
    Guid? AssignedAdminId,
    string? AssignedAdminName);
