namespace CommunityIncidentReporting.Infrastructure.Security;

public class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>HMAC key used to hash OTP codes before they're stored — never the raw code.</summary>
    public string HashKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 10;
    public int MaxAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 45;
}
