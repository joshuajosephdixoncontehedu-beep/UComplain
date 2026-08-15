namespace CommunityIncidentReporting.Infrastructure.Security;

/// <summary>
/// Bound from the same "Jwt" config section as JwtOptions (admin) but with distinct
/// property/env-var names (Jwt__ReporterSecret, etc.) and, critically, a distinct
/// signing secret — see IReporterJwtTokenGenerator.
/// </summary>
public class ReporterJwtOptions
{
    public const string SectionName = "Jwt";

    public string ReporterSecret { get; set; } = string.Empty;
    public string ReporterIssuer { get; set; } = string.Empty;
    public string ReporterAudience { get; set; } = string.Empty;
    public int ReporterAccessTokenMinutes { get; set; } = 15;
    public int ReporterRefreshTokenDays { get; set; } = 30;

    /// <summary>Refresh token lifetime when a login's RememberMe is false — a short session instead of the full 30 days.</summary>
    public int ReporterShortSessionHours { get; set; } = 12;
}
