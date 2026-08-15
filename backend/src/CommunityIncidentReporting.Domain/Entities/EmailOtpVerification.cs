using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A single issued OTP code, hashed — never the raw code. ReporterId is nullable
/// because a SignUpVerification OTP is issued before the Reporter row's email is
/// considered verified (the account already exists at that point, just inactive), and
/// because a PasswordReset flow must not reveal whether a Reporter with the given email
/// exists at all, so lookups are always by Email first.
/// </summary>
public class EmailOtpVerification
{
    public Guid Id { get; set; }
    public Guid? ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public string Email { get; set; } = string.Empty;
    public EmailOtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? RequestIp { get; set; }
    public string? UserAgent { get; set; }
}
