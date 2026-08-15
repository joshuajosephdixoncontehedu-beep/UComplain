using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// One row per consent-capture event — append-only, so a reporter's consent history
/// stays fully auditable. Re-granting (or, in a future phase, revoking) a given
/// ConsentType always inserts a new row rather than mutating an earlier one; the current
/// state for a ConsentType is whichever row has the latest GrantedAt. RevokedAt exists
/// for a future explicit-revoke flow — nothing sets it yet.
/// </summary>
public class ReporterConsent
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public ConsentType ConsentType { get; set; }
    public bool Granted { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
