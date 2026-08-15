using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

namespace CommunityIncidentReporting.Application.Features.MobileAuth;

public interface IReporterAuthService
{
    /// <summary>Creates an inactive, unverified Reporter and sends a sign-up OTP. Never issues a session.</summary>
    Task<RegisterReporterResponse> RegisterAsync(
        RegisterReporterRequest request, string? requestIp, string? userAgent, CancellationToken cancellationToken);

    /// <summary>Activates the account, marks the email verified, and issues a session.</summary>
    Task<ReporterAuthTokenResponse> VerifyEmailOtpAsync(
        VerifyEmailOtpRequest request, CancellationToken cancellationToken);

    /// <summary>Always completes successfully from the caller's perspective — cooldown/no-such-account never surface.</summary>
    Task ResendEmailOtpAsync(
        ResendEmailOtpRequest request, string? requestIp, string? userAgent, CancellationToken cancellationToken);

    /// <summary>Throws InvalidCredentialsException for wrong password, unverified, inactive, or restricted accounts — same message for all.</summary>
    Task<ReporterAuthTokenResponse> LoginAsync(ReporterLoginRequest request, CancellationToken cancellationToken);

    Task<ReporterAuthTokenResponse> RefreshAsync(ReporterRefreshTokenRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(ReporterRefreshTokenRequest request, CancellationToken cancellationToken);

    Task<ReporterProfileDto> GetProfileAsync(Guid reporterId, CancellationToken cancellationToken);

    /// <summary>Always completes successfully — never reveals whether the email is registered.</summary>
    Task ForgotPasswordAsync(
        ForgotPasswordRequest request, string? requestIp, string? userAgent, CancellationToken cancellationToken);

    /// <summary>Validates, without consuming, a password-reset code (throws InvalidOtpException if wrong/expired).</summary>
    Task VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequest request, CancellationToken cancellationToken);

    /// <summary>Re-validates and consumes the code, sets the new password, and revokes all existing reporter sessions.</summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    /// <summary>Records one or more consent grants (append-only — see ReporterConsent).</summary>
    Task<IReadOnlyList<ConsentDto>> RecordConsentAsync(
        Guid reporterId, RecordConsentRequest request, CancellationToken cancellationToken);
}
