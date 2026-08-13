using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reporters.Dtos;

public record ReporterListItemDto(
    Guid Id,
    string MaskedContactReference,
    VerificationStatus VerificationStatus,
    bool IsRestricted,
    DateTimeOffset? ConsentAt,
    int ReportCount,
    DateTimeOffset CreatedAt);
