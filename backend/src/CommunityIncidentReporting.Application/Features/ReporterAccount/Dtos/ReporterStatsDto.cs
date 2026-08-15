namespace CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;

/// <summary>Reuses MobileReports' ReportCountsDto bucket shape directly rather than redefining the same counts under a new name.</summary>
public record ReporterStatsDto(int ActiveReports, int ResolvedReports, int RejectedReports, int TotalReports, DateTimeOffset MemberSince);
