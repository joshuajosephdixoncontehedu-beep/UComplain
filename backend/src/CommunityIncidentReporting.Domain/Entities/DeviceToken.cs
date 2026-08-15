using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A push-notification device registration — persisted only, no push send in this phase
/// (see docs/mobile-client-backend-extension.md's Phase 6 notes). Token is globally
/// unique: re-registering an existing token reassigns it to the calling reporter rather
/// than erroring, matching how the same token naturally moves between accounts on a
/// shared device or after a reinstall/relogin.
/// </summary>
public class DeviceToken
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public DevicePlatform Platform { get; set; }
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
