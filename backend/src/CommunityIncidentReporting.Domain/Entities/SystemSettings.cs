namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// Singleton row (exactly one) holding organisation-wide admin portal configuration.
/// SettingsService gets-or-creates it with defaults, so no migration-time seeding is
/// required in any environment.
/// </summary>
public class SystemSettings
{
    public Guid Id { get; set; }

    // Organisation
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationContactEmail { get; set; } = string.Empty;

    // Notifications
    public bool NotifyOnNewVerifiedReport { get; set; }
    public bool NotifyOnCriticalPriority { get; set; }

    // Verification rules
    public int DefaultVerificationSlaHours { get; set; }
    public int DuplicateDetectionWindowHours { get; set; }

    // Privacy / data retention
    public int ReporterDataRetentionMonths { get; set; }
    public int AuditLogRetentionMonths { get; set; }

    // WhatsApp integration — placeholder only; the chatbot itself is not implemented.
    // See docs/whatsapp-integration-plan.md.
    public bool WhatsAppIntegrationEnabled { get; set; }
    public string? WhatsAppPlaceholderNote { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedByAdminId { get; set; }
}
