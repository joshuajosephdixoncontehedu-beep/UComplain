using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports;

/// <summary>
/// The single mapping function every reporter-facing endpoint uses to turn the admin
/// side's two-enum state (VerificationStatus + CaseStatus) into the mobile app's
/// five-stage tracker/badge/progress-bar/list-tab model (product brief §5) — so the
/// mapping only ever lives in one place, and the mobile client never has to reimplement
/// it. Deliberately does not introduce a third, unified status enum: VerificationStatus
/// and CaseStatus are exercised throughout IncidentReportService/VerificationService and
/// a wide existing test suite, so this projects from them instead of replacing them.
///
/// The product brief's own table has a "Verified" stage separate from "UnderReview", but
/// in this system VerificationService.DecideAsync's Approve action always sets
/// VerificationStatus=Verified and CaseStatus=UnderReview in the same transaction — there
/// is no reachable state with Verified but an earlier CaseStatus. Stage/progress numbers
/// below follow the brief's "UnderReview" row (stage 3, 60%) for that first-verified
/// state; the badge label says "Verified" since that's what actually happened from the
/// reporter's point of view.
/// </summary>
public static class ReportStatusProjection
{
    public static ReportStatusProjectionDto Project(VerificationStatus verificationStatus, CaseStatus caseStatus)
    {
        // Withdrawal can happen from several different underlying VerificationStatus
        // values (see Phase 4's withdraw rule) — check it first, independent of them.
        if (caseStatus == CaseStatus.Withdrawn)
        {
            return new ReportStatusProjectionDto("Withdrawn", TrackerStage: null, ProgressPercent: 0, ReportListBucket.NotListed);
        }

        return verificationStatus switch
        {
            VerificationStatus.Pending =>
                new ReportStatusProjectionDto("Awaiting verification", 1, 15, ReportListBucket.Active),

            // Paused, excluded from the officer's operational queue (VerificationStatus
            // != Verified already keeps it out of IncidentReportService.GetAllAsync) —
            // Phase 5 adds the message/reply/deadline machinery around this state.
            VerificationStatus.NeedsClarification =>
                new ReportStatusProjectionDto("Needs clarification", 1, 25, ReportListBucket.Active),

            // Escalated for abuse/duplicate review — same tier as NeedsClarification from
            // the reporter's point of view: still pending a human decision, not yet
            // rejected. Deliberately soft-worded rather than "flagged" or "escalated".
            VerificationStatus.FlaggedAbuse =>
                new ReportStatusProjectionDto("Under review", 1, 25, ReportListBucket.Active),

            VerificationStatus.Rejected =>
                new ReportStatusProjectionDto("Rejected", null, 0, ReportListBucket.Rejected),

            VerificationStatus.SuspectedDuplicate =>
                new ReportStatusProjectionDto("Duplicate", null, 0, ReportListBucket.Rejected),

            VerificationStatus.Verified => ProjectVerified(caseStatus),

            _ => new ReportStatusProjectionDto(verificationStatus.ToString(), null, 0, ReportListBucket.Active)
        };
    }

    private static ReportStatusProjectionDto ProjectVerified(CaseStatus caseStatus) => caseStatus switch
    {
        CaseStatus.UnderReview => new ReportStatusProjectionDto("Verified", 3, 60, ReportListBucket.Active),
        CaseStatus.Assigned => new ReportStatusProjectionDto("Assigned", 3, 70, ReportListBucket.Active),
        CaseStatus.InProgress => new ReportStatusProjectionDto("In progress", 4, 85, ReportListBucket.Active),
        CaseStatus.Resolved => new ReportStatusProjectionDto("Resolved", 5, 100, ReportListBucket.Resolved),
        CaseStatus.Closed => new ReportStatusProjectionDto("Closed", 5, 100, ReportListBucket.Resolved),
        CaseStatus.Rejected => new ReportStatusProjectionDto("Rejected", null, 0, ReportListBucket.Rejected),
        CaseStatus.Duplicate => new ReportStatusProjectionDto("Duplicate", null, 0, ReportListBucket.Rejected),
        // VerificationPending shouldn't co-occur with Verified in practice, but fall back
        // safely rather than throw if it ever does.
        _ => new ReportStatusProjectionDto("Verified", 2, 40, ReportListBucket.Active)
    };
}
