namespace CommunityIncidentReporting.Application.Features.Settings.Dtos;

public record UpdateSettingsRequest(
    string OrganizationName,
    string OrganizationContactEmail,
    bool NotifyOnNewVerifiedReport,
    bool NotifyOnCriticalPriority,
    int DefaultVerificationSlaHours,
    int DuplicateDetectionWindowHours,
    int ReporterDataRetentionMonths,
    int AuditLogRetentionMonths,
    string? WhatsAppPlaceholderNote);
