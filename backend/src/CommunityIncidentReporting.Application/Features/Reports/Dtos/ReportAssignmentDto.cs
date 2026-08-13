namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record ReportAssignmentDto(
    Guid Id,
    Guid AdminUserId,
    string AdminUserName,
    Guid AssignedByAdminId,
    string AssignedByAdminName,
    DateTimeOffset AssignedAt,
    DateTimeOffset? UnassignedAt);
