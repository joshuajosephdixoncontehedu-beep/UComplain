using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reporters.Dtos;

public record ReporterReportSummaryDto(
    Guid Id,
    string CaseReference,
    string CategoryName,
    CaseStatus CaseStatus,
    VerificationStatus VerificationStatus,
    DateTimeOffset CreatedAt);
