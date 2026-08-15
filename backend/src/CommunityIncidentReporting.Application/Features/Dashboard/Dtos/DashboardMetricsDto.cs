namespace CommunityIncidentReporting.Application.Features.Dashboard.Dtos;

public record DashboardMetricsDto(
    int TotalReportsReceived,
    int AwaitingVerification,
    int VerifiedAwaitingReview,
    int InProgress,
    int Resolved,
    int RejectedDuplicateOrFlagged,
    double? AverageVerificationTimeHours,
    double? AverageResolutionTimeHours,
    // Additive: total reports in range broken down by SourceChannel (e.g. "WhatsApp", "MobileApp").
    IReadOnlyList<NamedCountDto> ReportsBySourceChannel);
