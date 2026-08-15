namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// Reporter-scoped counterpart to <see cref="RefreshToken"/> (which is AdminUser-only).
/// Kept as a separate table rather than a shared one so the two actor types' sessions
/// can never be confused, and so RefreshToken's non-nullable AdminUserId FK doesn't need
/// to become nullable. Only a hash of the token is stored, never the raw value.
/// </summary>
public class ReporterRefreshToken
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>
    /// Whether the login that produced this token had RememberMe=true (long-lived) or
    /// false (short session). Carried forward on rotation (see
    /// ReporterAuthService.RefreshAsync) so a short session never silently becomes a
    /// long-lived one just by refreshing.
    /// </summary>
    public bool IsRemembered { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
