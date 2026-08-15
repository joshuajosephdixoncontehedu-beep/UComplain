using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// InitialPrioritySignal is accepted for context (recorded in the audit log) but never
/// determines the stored Priority — the server always derives that from the category's
/// DefaultPriority, exactly like the WhatsApp intake path.
/// </summary>
public record CreateMobileReportRequest(
    Guid CategoryId,
    string Description,
    DateTimeOffset IncidentOccurredAt,
    string LocationDescription,
    double? Latitude,
    double? Longitude,
    IncidentPriority? InitialPrioritySignal,
    string? Landmark = null);
