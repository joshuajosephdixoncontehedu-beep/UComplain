namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

public record RegisterReporterResponse(Guid ReporterId, string Email, bool VerificationRequired, string Message);
