namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

public record VerifyPasswordResetOtpRequest(string Email, string OtpCode);
