using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reporters.Dtos;

public record ReporterVerificationEventDto(
    Guid Id,
    Guid IncidentReportId,
    string CaseReference,
    VerificationDecisionResult Result,
    string? Notes,
    DateTimeOffset CreatedAt);
