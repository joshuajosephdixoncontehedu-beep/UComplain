using System.Security.Cryptography;
using System.Text;
using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class EmailOtpService(AppDbContext db, IOptions<OtpOptions> options) : IEmailOtpService
{
    private const string InvalidOtpMessage = "Invalid or expired code.";
    private const string TooManyAttemptsMessage = "Too many incorrect attempts. Please request a new code.";

    public async Task<string> IssueAsync(
        string email, EmailOtpPurpose purpose, Guid? reporterId,
        string? requestIp, string? userAgent, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var opts = options.Value;
        var now = DateTimeOffset.UtcNow;

        // Invalidate any still-active OTPs for this email+purpose so only the most
        // recently issued code can ever be verified.
        var activeOtps = await db.EmailOtpVerifications
            .Where(o => o.Email == normalizedEmail && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var otp in activeOtps)
        {
            otp.IsUsed = true;
            otp.UsedAt = now;
        }

        var code = GenerateSixDigitCode();

        db.EmailOtpVerifications.Add(new EmailOtpVerification
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            Email = normalizedEmail,
            Purpose = purpose,
            CodeHash = Hash(code, opts.HashKey),
            ExpiresAt = now.AddMinutes(opts.ExpiryMinutes),
            AttemptCount = 0,
            MaxAttempts = opts.MaxAttempts,
            IsUsed = false,
            CreatedAt = now,
            RequestIp = requestIp,
            UserAgent = userAgent
        });

        await db.SaveChangesAsync(cancellationToken);
        return code;
    }

    public async Task<bool> CanIssueAsync(string email, EmailOtpPurpose purpose, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var opts = options.Value;
        var cooldownStart = DateTimeOffset.UtcNow.AddSeconds(-opts.ResendCooldownSeconds);

        var lastIssuedAt = await db.EmailOtpVerifications
            .Where(o => o.Email == normalizedEmail && o.Purpose == purpose)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (DateTimeOffset?)o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return lastIssuedAt is null || lastIssuedAt < cooldownStart;
    }

    public async Task VerifyAsync(
        string email, EmailOtpPurpose purpose, string code, bool consume, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        var otp = await db.EmailOtpVerifications
            .Where(o => o.Email == normalizedEmail && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            throw new InvalidOtpException(InvalidOtpMessage);
        }

        if (otp.AttemptCount >= otp.MaxAttempts)
        {
            throw new InvalidOtpException(TooManyAttemptsMessage);
        }

        var providedHash = Hash(code, options.Value.HashKey);
        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedHash), Encoding.UTF8.GetBytes(otp.CodeHash));

        if (!matches)
        {
            otp.AttemptCount++;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOtpException(InvalidOtpMessage);
        }

        if (consume)
        {
            otp.IsUsed = true;
            otp.UsedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSixDigitCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string Hash(string code, string hashKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hashKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(code))).ToLowerInvariant();
    }
}
