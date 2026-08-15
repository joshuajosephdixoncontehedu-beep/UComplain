using CommunityIncidentReporting.Domain.Entities;

namespace CommunityIncidentReporting.Application.Common.Interfaces;

/// <summary>
/// Recomputes and sets IncidentReport.IsPubliclyVisible in place from the report's
/// current VerificationStatus/CaseStatus and its reporter's ShowOnPublicMap privacy
/// setting (defaults true when the reporter has no ReporterPrivacySetting row yet — same
/// default as that entity's own property initializer). Same "mutate, don't save" pattern
/// as IAuditLogger/INotificationService — the caller's own SaveChangesAsync persists it.
/// Called from every relevant transition point: VerificationService.DecideAsync,
/// IncidentReportService.ChangeStatusAsync/AssignAsync, MobileReportService.WithdrawAsync,
/// and ClarificationAutoCloseService. This recomputed flag is a defense-in-depth/
/// performance aid, not the sole gate — the public map query (Phase 7) re-checks
/// VerificationStatus/CaseStatus/ShowOnPublicMap directly too, rather than trusting a
/// possibly-stale cached value alone.
/// </summary>
public interface IReportVisibilityService
{
    Task RecomputeAsync(IncidentReport report, CancellationToken cancellationToken);
}
