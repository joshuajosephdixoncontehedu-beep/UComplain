namespace CommunityIncidentReporting.Application.Features.Analytics.Dtos;

public record AssignmentWorkloadDto(
    Guid AdminId, string AdminName, int OpenAssignedCount, int ResolvedInRangeCount);
