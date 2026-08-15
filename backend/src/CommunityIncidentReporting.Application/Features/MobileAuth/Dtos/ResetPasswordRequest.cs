namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

public record ResetPasswordRequest(string Email, string OtpCode, string NewPassword, string ConfirmNewPassword);
