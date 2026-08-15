namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

/// <summary>
/// Deliberately generic response for endpoints that must never reveal whether an email
/// exists, is verified, or has a pending OTP (resend-email-otp, forgot-password,
/// verify-password-reset-otp).
/// </summary>
public record GenericMessageResponse(string Message);
