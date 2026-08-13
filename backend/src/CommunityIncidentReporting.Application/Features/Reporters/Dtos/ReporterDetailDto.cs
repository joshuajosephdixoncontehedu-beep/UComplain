using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reporters.Dtos;

public record ReporterDetailDto(
    Guid Id,
    string MaskedContactReference,
    VerificationStatus VerificationStatus,
    bool IsRestricted,
    DateTimeOffset? ConsentAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ReporterReportSummaryDto> Reports,
    IReadOnlyList<ReporterVerificationEventDto> VerificationHistory);
