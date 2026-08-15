using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// Full-replace semantics for every mutable draft field — same "PATCH route, full-object
/// replacement" convention as the admin side's UpdateReportRequest, not a partial diff.
/// Send the wizard's complete current state on every call, not just the field that
/// changed on this step. All fields are optional since a draft fills in incrementally.
/// </summary>
public record UpdateDraftRequest(
    Guid? CategoryId,
    string? Description,
    DateTimeOffset? IncidentOccurredAt,
    IncidentPriority? InitialPrioritySignal,
    string? LocationDescription,
    double? Latitude,
    double? Longitude,
    string? Landmark);
