namespace CommunityIncidentReporting.Application.Features.Settings.Dtos;

public record SettingsDto(
    string OrganizationName,
    string OrganizationContactEmail,
    bool NotifyOnNewVerifiedReport,
    bool NotifyOnCriticalPriority,
    int DefaultVerificationSlaHours,
    int DuplicateDetectionWindowHours,
    int ReporterDataRetentionMonths,
    int AuditLogRetentionMonths,
    bool WhatsAppIntegrationEnabled,
    string? WhatsAppPlaceholderNote,
    DateTimeOffset UpdatedAt);
