using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Common.Interfaces;

/// <summary>
/// Issues and validates six-digit email OTP codes (EmailOtpVerification rows). Shared by
/// sign-up verification, password reset, and (in future) email-change flows — each a
/// distinct EmailOtpPurpose so a code issued for one purpose can never be replayed for
/// another, even for the same email.
/// </summary>
public interface IEmailOtpService
{
    /// <summary>
    /// Invalidates any still-active OTP for the same email+purpose, creates a new one,
    /// and returns the raw (unhashed) code for the caller to embed in the outgoing email
    /// — the raw code is never itself persisted.
    /// </summary>
    Task<string> IssueAsync(
        string email, EmailOtpPurpose purpose, Guid? reporterId,
        string? requestIp, string? userAgent, CancellationToken cancellationToken);

    /// <summary>
    /// False if a still-active OTP for this email+purpose was issued within the
    /// configured resend cooldown window — callers must still return a generic response
    /// either way (never reveal whether cooldown is active).
    /// </summary>
    Task<bool> CanIssueAsync(string email, EmailOtpPurpose purpose, CancellationToken cancellationToken);

    /// <summary>
    /// Validates a code against the latest active OTP for the given email+purpose.
    /// Throws InvalidOtpException for any failure (missing/expired/wrong/attempt-limited)
    /// — always the same generic message, so a caller can't distinguish failure reasons.
    /// When <paramref name="consume"/> is true (sign-up verification, password reset),
    /// the OTP is marked used and can never be validated again. When false (the
    /// password-reset "verify code" step, which precedes a separate reset-password call
    /// that resupplies and consumes the same code), a wrong-attempt still counts toward
    /// MaxAttempts but a correct one does not consume the code.
    /// </summary>
    Task VerifyAsync(
        string email, EmailOtpPurpose purpose, string code, bool consume, CancellationToken cancellationToken);
}
