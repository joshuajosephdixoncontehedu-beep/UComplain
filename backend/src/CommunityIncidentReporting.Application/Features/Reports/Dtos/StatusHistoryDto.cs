using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record StatusHistoryDto(
    Guid Id,
    CaseStatus PreviousStatus,
    CaseStatus NewStatus,
    Guid ChangedByAdminId,
    string ChangedByAdminName,
    string? Notes,
    DateTimeOffset CreatedAt);
