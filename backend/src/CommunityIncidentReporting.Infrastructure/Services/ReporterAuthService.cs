using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileAuth;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Email;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class ReporterAuthService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IReporterJwtTokenGenerator jwtTokenGenerator,
    IOptions<ReporterJwtOptions> jwtOptions,
    IOptions<OtpOptions> otpOptions,
    IEmailOtpService emailOtpService,
    IEmailService emailService,
    IAuditLogger auditLogger,
    ILogger<ReporterAuthService> logger) : IReporterAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";
    private const string InvalidRefreshTokenMessage = "Invalid or expired refresh token.";
    private const string GenericOtpFailureMessage = "Invalid or expired code.";

    public async Task<RegisterReporterResponse> RegisterAsync(
        RegisterReporterRequest request, string? requestIp, string? userAgent, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        var reporter = await db.Reporters
            .FirstOrDefaultAsync(r => r.NormalizedEmail == normalizedEmail, cancellationToken);

        if (reporter is not null && reporter.EmailVerifiedAt is not null)
        {
            throw new BusinessRuleException("An account with this email already exists.");
        }

        if (reporter is null)
        {
            reporter = new Reporter
            {
                Id = Guid.NewGuid(),
                WhatsAppNumberHash = string.Empty,
                MaskedContactReference = string.Empty,
                CreatedAt = now
            };
            db.Reporters.Add(reporter);
        }

        // Either a brand-new row, or resuming a previous incomplete (unverified)
        // registration for the same email — either way the latest submitted details win.
        reporter.FullName = request.FullName.Trim();
        reporter.Email = request.Email.Trim();
        reporter.NormalizedEmail = normalizedEmail;
        reporter.PhoneNumber = request.PhoneNumber.Trim();
        reporter.PasswordHash = passwordHasher.Hash(request.Password);
        reporter.IsActive = false;
        reporter.ConsentAt = now;
        reporter.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);

        var code = await emailOtpService.IssueAsync(
            normalizedEmail, EmailOtpPurpose.SignUpVerification, reporter.Id, requestIp, userAgent, cancellationToken);

        var (subject, html, text) = EmailTemplates.EmailVerificationOtp(reporter.FullName, code, otpOptions.Value.ExpiryMinutes);
        await emailService.SendAsync(new EmailMessage(reporter.Email, subject, html, text), cancellationToken);

        await auditLogger.LogAsync(
            adminUserId: null, "ReporterRegistered", nameof(Reporter), reporter.Id.ToString(),
            previousValue: null, newValue: new { reporter.Email }, requestIp, userAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new RegisterReporterResponse(
            reporter.Id, normalizedEmail, true, "Registration received. Check your email for a verification code.");
    }

    public async Task<ReporterAuthTokenResponse> VerifyEmailOtpAsync(
        VerifyEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var reporter = await db.Reporters
            .FirstOrDefaultAsync(r => r.NormalizedEmail == normalizedEmail, cancellationToken)
            ?? throw new InvalidOtpException(GenericOtpFailureMessage);

        await emailOtpService.VerifyAsync(
            normalizedEmail, EmailOtpPurpose.SignUpVerification, request.OtpCode, consume: true, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        reporter.EmailVerifiedAt = now;
        reporter.IsActive = true;
        reporter.ConsentAt ??= now;
        reporter.UpdatedAt = now;

        var response = await IssueTokenPairAsync(reporter, cancellationToken);

        await auditLogger.LogAsync(
            adminUserId: null, "ReporterEmailVerified", nameof(Reporter), reporter.Id.ToString(),
            previousValue: null, newValue: new { reporter.Email }, ipAddress: null, userAgent: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await SendWelcomeEmailBestEffortAsync(reporter, cancellationToken);

        return response;
    }

    public async Task ResendEmailOtpAsync(
        ResendEmailOtpRequest request, string? requestIp, string? userAgent, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var reporter = await db.Reporters
            .FirstOrDefaultAsync(r => r.NormalizedEmail == normalizedEmail && r.EmailVerifiedAt == null, cancellationToken);

        if (reporter is null)
        {
            return;
        }

        if (!await emailOtpService.CanIssueAsync(normalizedEmail, EmailOtpPurpose.SignUpVerification, cancellationToken))
        {
            return;
        }

        var code = await emailOtpService.IssueAsync(
            normalizedEmail, EmailOtpPurpose.SignUpVerification, reporter.Id, requestIp, userAgent, cancellationToken);

        try
        {
            var (subject, html, text) =
                EmailTemplates.EmailVerificationOtp(reporter.FullName ?? "there", code, otpOptions.Value.ExpiryMinutes);
            await emailService.SendAsync(new EmailMessage(reporter.Email!, subject, html, text), cancellationToken);
        }
        catch (EmailDeliveryException ex)
        {
            // The caller still gets a generic success response either way — surfacing
            // this would let a caller distinguish "email exists and resend failed" from
            // "nothing happened", defeating the point of the generic response.
            logger.LogWarning(ex, "Failed to send resend-OTP email for a sign-up verification request.");
        }
    }

    public async Task<ReporterAuthTokenResponse> LoginAsync(ReporterLoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var reporter = await db.Reporters
            .FirstOrDefaultAsync(r => r.NormalizedEmail == normalizedEmail, cancellationToken);

        if (reporter is null || !reporter.IsActive || reporter.IsRestricted || reporter.EmailVerifiedAt is null
            || reporter.PasswordHash is null || !passwordHasher.Verify(request.Password, reporter.PasswordHash))
        {
            throw new InvalidCredentialsException(InvalidCredentialsMessage);
        }

        reporter.LastLoginAt = DateTimeOffset.UtcNow;
        reporter.UpdatedAt = DateTimeOffset.UtcNow;

        var response = await IssueTokenPairAsync(reporter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<ReporterAuthTokenResponse> RefreshAsync(
        ReporterRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenGenerator.Hash(request.RefreshToken);
        var existingToken = await db.ReporterRefreshTokens
            .Include(t => t.Reporter)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive || existingToken.Reporter is null
            || !existingToken.Reporter.IsActive || existingToken.Reporter.IsRestricted)
        {
            throw new InvalidCredentialsException(InvalidRefreshTokenMessage);
        }

        var response = await IssueTokenPairAsync(existingToken.Reporter, cancellationToken, revoking: existingToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(ReporterRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenGenerator.Hash(request.RefreshToken);
        var existingToken = await db.ReporterRefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken is { RevokedAt: null })
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ReporterProfileDto> GetProfileAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var reporter = await db.Reporters.FindAsync([reporterId], cancellationToken)
            ?? throw new NotFoundException(nameof(Reporter), reporterId);

        return ToProfileDto(reporter);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request, string? requestIp, string? userAgent, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var reporter = await db.Reporters.FirstOrDefaultAsync(
            r => r.NormalizedEmail == normalizedEmail && r.EmailVerifiedAt != null && r.IsActive, cancellationToken);

        if (reporter is null)
        {
            return;
        }

        if (!await emailOtpService.CanIssueAsync(normalizedEmail, EmailOtpPurpose.PasswordReset, cancellationToken))
        {
            return;
        }

        var code = await emailOtpService.IssueAsync(
            normalizedEmail, EmailOtpPurpose.PasswordReset, reporter.Id, requestIp, userAgent, cancellationToken);

        try
        {
            var (subject, html, text) =
                EmailTemplates.PasswordResetOtp(reporter.FullName ?? "there", code, otpOptions.Value.ExpiryMinutes);
            await emailService.SendAsync(new EmailMessage(reporter.Email!, subject, html, text), cancellationToken);
        }
        catch (EmailDeliveryException ex)
        {
            logger.LogWarning(ex, "Failed to send password-reset OTP email.");
        }
    }

    public async Task VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        await emailOtpService.VerifyAsync(
            normalizedEmail, EmailOtpPurpose.PasswordReset, request.OtpCode, consume: false, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        await emailOtpService.VerifyAsync(
            normalizedEmail, EmailOtpPurpose.PasswordReset, request.OtpCode, consume: true, cancellationToken);

        var reporter = await db.Reporters.FirstOrDefaultAsync(r => r.NormalizedEmail == normalizedEmail, cancellationToken)
            ?? throw new InvalidOtpException(GenericOtpFailureMessage);

        var now = DateTimeOffset.UtcNow;
        reporter.PasswordHash = passwordHasher.Hash(request.NewPassword);
        reporter.UpdatedAt = now;

        var activeTokens = await db.ReporterRefreshTokens
            .Where(t => t.ReporterId == reporter.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await auditLogger.LogAsync(
            adminUserId: null, "ReporterPasswordReset", nameof(Reporter), reporter.Id.ToString(),
            previousValue: null, newValue: null, ipAddress: null, userAgent: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ReporterAuthTokenResponse> IssueTokenPairAsync(
        Reporter reporter, CancellationToken cancellationToken, ReporterRefreshToken? revoking = null)
    {
        var (accessToken, accessExpiresAt) = jwtTokenGenerator.GenerateAccessToken(reporter);

        var rawRefreshToken = RefreshTokenGenerator.GenerateRawToken();
        var refreshTokenHash = RefreshTokenGenerator.Hash(rawRefreshToken);
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.ReporterRefreshTokenDays);

        var newToken = new ReporterRefreshToken
        {
            Id = Guid.NewGuid(),
            ReporterId = reporter.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (revoking is not null)
        {
            revoking.RevokedAt = DateTimeOffset.UtcNow;
            revoking.ReplacedByTokenHash = refreshTokenHash;
        }

        db.ReporterRefreshTokens.Add(newToken);
        await Task.CompletedTask;

        return new ReporterAuthTokenResponse(
            accessToken, accessExpiresAt, rawRefreshToken, refreshExpiresAt, ToProfileDto(reporter));
    }

    private async Task SendWelcomeEmailBestEffortAsync(Reporter reporter, CancellationToken cancellationToken)
    {
        try
        {
            var (subject, html, text) = EmailTemplates.Welcome(reporter.FullName ?? "there");
            await emailService.SendAsync(new EmailMessage(reporter.Email!, subject, html, text), cancellationToken);
        }
        catch (EmailDeliveryException ex)
        {
            logger.LogWarning(ex, "Failed to send welcome email after email verification.");
        }
    }

    private static ReporterProfileDto ToProfileDto(Reporter reporter) => new(
        reporter.Id,
        reporter.FullName ?? string.Empty,
        reporter.Email ?? string.Empty,
        reporter.PhoneNumber,
        reporter.EmailVerifiedAt is not null,
        reporter.IsActive,
        reporter.IsRestricted,
        reporter.LastLoginAt,
        reporter.CreatedAt);
}
