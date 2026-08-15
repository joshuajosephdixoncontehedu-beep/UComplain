namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

public record VerifyEmailOtpRequest(string Email, string OtpCode);
