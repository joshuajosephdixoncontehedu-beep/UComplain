namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// Server-side record of an issued refresh token, so tokens can be revoked
/// (logout, admin deactivation) rather than relying purely on client-side deletion.
/// Only a hash of the token is stored, never the raw value.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid AdminUserId { get; set; }
    public AdminUser? AdminUser { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
