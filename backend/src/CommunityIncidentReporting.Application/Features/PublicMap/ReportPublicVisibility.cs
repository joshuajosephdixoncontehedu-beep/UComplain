using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.PublicMap;

/// <summary>
/// The single pure function behind IncidentReport.IsPubliclyVisible (see that property's
/// doc comment) — a report is safe to surface on the anonymous public map only when it's
/// Verified, not Withdrawn (the only "terminal/paused state that shouldn't be shown"
/// reachable while VerificationStatus stays Verified — Rejected/Duplicate can never
/// co-occur with Verified in this state machine, see VerificationService.DecideAsync),
/// and the reporter's own ShowOnPublicMap privacy setting allows it.
/// </summary>
public static class ReportPublicVisibility
{
    public static bool Compute(VerificationStatus verificationStatus, CaseStatus caseStatus, bool showOnPublicMap) =>
        showOnPublicMap && verificationStatus == VerificationStatus.Verified && caseStatus != CaseStatus.Withdrawn;
}
