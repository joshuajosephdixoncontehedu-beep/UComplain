namespace CommunityIncidentReporting.Application.Features.Dashboard.Dtos;

public record DashboardMetricsDto(
    int TotalReportsReceived,
    int AwaitingVerification,
    int VerifiedAwaitingReview,
    int InProgress,
    int Resolved,
    int RejectedDuplicateOrFlagged,
    double? AverageVerificationTimeHours,
    double? AverageResolutionTimeHours);
