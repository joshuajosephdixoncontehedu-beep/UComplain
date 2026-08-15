namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// One row per Reporter (get-or-create — same pattern as the SystemSettings singleton).
/// Only meaningful for mobile-app reporters; WhatsApp-only reporters never read/write
/// this. Every field here is a hard gate, not a UI preference: e.g. the public map query
/// (Phase 7) must never return a report whose reporter has ShowOnPublicMap = false,
/// regardless of the report's own VerificationStatus.
/// </summary>
public class ReporterPrivacySetting
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public bool UsePreciseLocation { get; set; } = true;
    public bool ShowOnPublicMap { get; set; } = true;
    public bool AllowResponderContact { get; set; } = true;

    public DateTimeOffset UpdatedAt { get; set; }
}
