using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Common.Interfaces;

/// <summary>
/// Adds a reporter-facing Notification row to the current DbContext — same "add, don't
/// save" pattern as IAuditLogger, so the caller's own SaveChangesAsync persists it in the
/// same transaction as whatever state change triggered it. Called from every relevant
/// transition point (VerificationService.DecideAsync, IncidentReportService.ChangeStatusAsync/
/// AssignAsync, ClarificationAutoCloseService). Persistence only in this phase — no
/// actual push send (see docs/mobile-client-backend-extension.md's Phase 6 notes).
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(
        Guid reporterId, NotificationType type, string title, string body, Guid? reportId,
        CancellationToken cancellationToken);
}
