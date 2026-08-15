using CommunityIncidentReporting.Application.Common.Models;

namespace CommunityIncidentReporting.Application.Common.Interfaces;

/// <summary>
/// Sends transactional email via whatever provider is configured (Resend in
/// Infrastructure). Throws EmailDeliveryException — mapped to a safe 502 response by
/// GlobalExceptionHandler — on any provider failure, so callers (registration, OTP
/// resend, password reset) surface a clear error rather than silently promising an email
/// that will never arrive.
/// </summary>
public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
