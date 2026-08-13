using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Dashboard.Dtos;

public record PriorityReportItemDto(
    Guid Id,
    string CaseReference,
    string CategoryName,
    string LocationDescription,
    IncidentPriority Priority,
    CaseStatus CaseStatus,
    string? AssignedAdminName,
    DateTimeOffset CreatedAt);
