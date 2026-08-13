using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Verification.Dtos;

public record VerificationQueueItemDto(
    Guid Id,
    string CaseReference,
    string CategoryName,
    string LocationDescription,
    IncidentPriority Priority,
    VerificationStatus VerificationStatus,
    int CategorySlaHours,
    DateTimeOffset CreatedAt,
    int AttemptCount,
    string ReporterMaskedContact);
