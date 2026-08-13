using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record VerificationEventDto(
    Guid Id,
    VerificationMethod VerificationMethod,
    VerificationDecisionResult Result,
    int AttemptNumber,
    string? Notes,
    Guid? PerformedByAdminId,
    string? PerformedByAdminName,
    DateTimeOffset CreatedAt);
