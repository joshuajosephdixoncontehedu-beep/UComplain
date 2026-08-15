using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.Clarifications;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

/// <summary>
/// The sweep behind Phase 5's clarification auto-close rule. Deliberately writes no
/// StatusHistory row for the CaseStatus transition — StatusHistory.ChangedByAdminId is a
/// required FK to AdminUser and this is a system action with no admin actor (same
/// reasoning as MobileReportService.WithdrawAsync in Phase 4). Unlike that case,
/// VerificationEvent already has first-class support for automated/non-human events
/// (PerformedByAdminId is nullable, and VerificationMethod.AutomatedDuplicateCheck already
/// preceded this), so the automated transition is recorded there instead.
/// </summary>
public class ClarificationAutoCloseService(
    AppDbContext db, IAuditLogger auditLogger, INotificationService notificationService,
    IReportVisibilityService visibilityService) : IClarificationAutoCloseService
{
    private const string AutoCloseResolutionSummary =
        "Automatically closed — the reporter did not respond to a clarification request within the deadline.";

    public async Task<int> CloseOverdueAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // Guarded by IncidentReport.VerificationStatus == NeedsClarification so a report
        // an admin already re-decided through another path (approved/rejected/etc. after
        // the reporter answered by phone, say) is never touched here, even if nobody ever
        // explicitly marked this specific ClarificationRequest resolved.
        var overdue = await db.ClarificationRequests
            .Include(c => c.IncidentReport)
            .Where(c => c.ResolvedAt == null && c.AutoClosedAt == null && c.DueAt < now
                        && c.IncidentReport!.VerificationStatus == VerificationStatus.NeedsClarification)
            .ToListAsync(cancellationToken);

        foreach (var clarification in overdue)
        {
            var report = clarification.IncidentReport!;
            var previousVerificationStatus = report.VerificationStatus;
            var previousCaseStatus = report.CaseStatus;

            // Rejected (not left as NeedsClarification) so ReportStatusProjection reflects
            // finality correctly — NeedsClarification's projection branch doesn't consult
            // CaseStatus at all, so leaving VerificationStatus unchanged here would show a
            // stale "Needs clarification"/Active badge on a report that's actually Closed.
            report.VerificationStatus = VerificationStatus.Rejected;
            report.CaseStatus = CaseStatus.Closed;
            report.ClosedAt = now;
            report.ResolutionSummary = AutoCloseResolutionSummary;
            report.UpdatedAt = now;

            clarification.AutoClosedAt = now;

            await visibilityService.RecomputeAsync(report, cancellationToken);

            var attemptNumber = await db.VerificationEvents.CountAsync(v => v.IncidentReportId == report.Id, cancellationToken) + 1;
            db.VerificationEvents.Add(new VerificationEvent
            {
                Id = Guid.NewGuid(),
                IncidentReportId = report.Id,
                ReporterId = report.ReporterId,
                VerificationMethod = VerificationMethod.AutomatedClarificationTimeout,
                Result = VerificationDecisionResult.AutoClosed,
                AttemptNumber = attemptNumber,
                Notes = $"No reporter response to the clarification request by its {clarification.DueAt:u} deadline.",
                PerformedByAdminId = null,
                CreatedAt = now
            });

            await auditLogger.LogAsync(
                adminUserId: null, "ReportAutoClosedForUnansweredClarification", nameof(IncidentReport),
                report.Id.ToString(),
                previousValue: new
                {
                    VerificationStatus = previousVerificationStatus.ToString(), CaseStatus = previousCaseStatus.ToString()
                },
                newValue: new
                {
                    VerificationStatus = report.VerificationStatus.ToString(), CaseStatus = report.CaseStatus.ToString(),
                    ClarificationRequestId = clarification.Id
                },
                ipAddress: null, userAgent: null, cancellationToken);

            await notificationService.NotifyAsync(
                report.ReporterId, NotificationType.ReportAutoClosed, "Report closed",
                $"Your report {report.CaseReference} was automatically closed because we didn't receive a response " +
                "to our request for more information.", report.Id, cancellationToken);
        }

        if (overdue.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return overdue.Count;
    }
}
