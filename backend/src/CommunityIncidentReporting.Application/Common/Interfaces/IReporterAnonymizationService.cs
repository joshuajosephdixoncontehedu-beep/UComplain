using CommunityIncidentReporting.Domain.Entities;

namespace CommunityIncidentReporting.Application.Common.Interfaces;

/// <summary>
/// Scrubs a Reporter's PII in place and revokes their sessions/device tokens — shared by
/// AccountDeletionProcessorService (a reporter's own grace-period-expired deletion
/// request) and ReporterRetentionPurgeService (SystemSettings.ReporterDataRetentionMonths
/// aging out an inactive account), so the two paths can never drift apart. Sets
/// Reporter.AnonymizedAt (idempotent — a no-op if already set). Does not call
/// SaveChangesAsync — same "mutate, don't save" pattern as IAuditLogger. IncidentReport
/// rows are never touched: kept intact for audit/legal continuity.
/// </summary>
public interface IReporterAnonymizationService
{
    Task AnonymizeAsync(Reporter reporter, CancellationToken cancellationToken);
}
