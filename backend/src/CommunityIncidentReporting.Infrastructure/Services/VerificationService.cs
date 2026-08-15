using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Reports;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using CommunityIncidentReporting.Application.Features.Verification;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Verification;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class VerificationService(
    AppDbContext db, IAuditLogger auditLogger, IIncidentReportService reportService,
    INotificationService notificationService, IReportVisibilityService visibilityService,
    IOptions<ClarificationOptions> clarificationOptions) : IVerificationService
{
    public async Task<VerificationQueueResponse> GetQueueAsync(CancellationToken cancellationToken)
    {
        // Five separate filtered queries rather than one GroupBy — EF Core's
        // translation of GroupBy over an enum-as-string column is unreliable, and
        // discrete per-tab queries are simpler and let empty tabs short-circuit.
        var pending = await GetTabAsync(VerificationStatus.Pending, cancellationToken);
        var needsClarification = await GetTabAsync(VerificationStatus.NeedsClarification, cancellationToken);
        var suspectedDuplicate = await GetTabAsync(VerificationStatus.SuspectedDuplicate, cancellationToken);
        var flaggedAbuse = await GetTabAsync(VerificationStatus.FlaggedAbuse, cancellationToken);
        var rejected = await GetTabAsync(VerificationStatus.Rejected, cancellationToken);

        return new VerificationQueueResponse(pending, needsClarification, suspectedDuplicate, flaggedAbuse, rejected);
    }

    private async Task<IReadOnlyList<VerificationQueueItemDto>> GetTabAsync(
        VerificationStatus status, CancellationToken cancellationToken) =>
        await db.IncidentReports
            .Where(r => r.VerificationStatus == status)
            .OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            .Select(r => new VerificationQueueItemDto(
                r.Id, r.CaseReference, r.Category!.Name, r.LocationDescription, r.Priority, r.VerificationStatus,
                r.Category!.SlaHours, r.CreatedAt, r.VerificationEvents.Count, r.Reporter!.MaskedContactReference,
                r.SourceChannel))
            .ToListAsync(cancellationToken);

    public async Task<IncidentReportDetailDto> DecideAsync(
        Guid id, VerificationDecisionRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var report = await db.IncidentReports.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(IncidentReport), id);

        if (report.VerificationStatus == VerificationStatus.Verified)
        {
            throw new BusinessRuleException("This report has already been verified.");
        }

        var (newVerificationStatus, newCaseStatus, result) = request.Action switch
        {
            VerificationDecisionAction.Approve =>
                (VerificationStatus.Verified, CaseStatus.UnderReview, VerificationDecisionResult.Approved),
            VerificationDecisionAction.Reject =>
                (VerificationStatus.Rejected, CaseStatus.Rejected, VerificationDecisionResult.Rejected),
            VerificationDecisionAction.RequestClarification =>
                (VerificationStatus.NeedsClarification, CaseStatus.VerificationPending, VerificationDecisionResult.ClarificationRequested),
            VerificationDecisionAction.MarkDuplicate =>
                (VerificationStatus.SuspectedDuplicate, CaseStatus.Duplicate, VerificationDecisionResult.MarkedDuplicate),
            VerificationDecisionAction.Escalate =>
                (VerificationStatus.FlaggedAbuse, CaseStatus.VerificationPending, VerificationDecisionResult.Escalated),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Action, "Unknown verification decision action.")
        };

        var now = DateTimeOffset.UtcNow;
        var previousVerificationStatus = report.VerificationStatus;
        var previousCaseStatus = report.CaseStatus;

        var attemptNumber = await db.VerificationEvents.CountAsync(v => v.IncidentReportId == id, cancellationToken) + 1;
        db.VerificationEvents.Add(new VerificationEvent
        {
            Id = Guid.NewGuid(),
            IncidentReportId = id,
            ReporterId = report.ReporterId,
            VerificationMethod = VerificationMethod.AdminReview,
            Result = result,
            AttemptNumber = attemptNumber,
            Notes = request.Reason,
            PerformedByAdminId = context.AdminUserId,
            CreatedAt = now
        });

        if (request.Action == VerificationDecisionAction.RequestClarification)
        {
            // request.Reason is required for this action — see
            // VerificationDecisionRequestValidator — so it's safe to use directly as the
            // reporter-facing clarification message, not just an internal note.
            db.ClarificationRequests.Add(new ClarificationRequest
            {
                Id = Guid.NewGuid(),
                IncidentReportId = id,
                RequestedByAdminId = context.AdminUserId,
                Message = request.Reason!,
                RequestedAt = now,
                DueAt = now.AddHours(clarificationOptions.Value.DeadlineHours)
            });
        }

        var notification = request.Action switch
        {
            VerificationDecisionAction.Approve =>
                ((NotificationType Type, string Title, string Body)?)(NotificationType.ReportVerified, "Report verified",
                    $"Your report {report.CaseReference} has been verified and is now under review."),
            VerificationDecisionAction.Reject =>
                (NotificationType.ReportRejected, "Report rejected",
                    $"Your report {report.CaseReference} was not accepted: {request.Reason}"),
            VerificationDecisionAction.RequestClarification =>
                (NotificationType.ClarificationRequested, "More information needed",
                    $"We need more information about your report {report.CaseReference}: {request.Reason}"),
            VerificationDecisionAction.MarkDuplicate =>
                (NotificationType.ReportClosedDuplicate, "Report marked as a duplicate",
                    $"Your report {report.CaseReference} was marked as a duplicate of an existing report."),
            // Escalate (FlaggedAbuse) is an internal review flag, not a reporter-facing event.
            _ => null
        };
        if (notification is { } n)
        {
            await notificationService.NotifyAsync(report.ReporterId, n.Type, n.Title, n.Body, report.Id, cancellationToken);
        }

        report.VerificationStatus = newVerificationStatus;

        if (report.CaseStatus != newCaseStatus)
        {
            db.StatusHistories.Add(new StatusHistory
            {
                Id = Guid.NewGuid(),
                IncidentReportId = id,
                PreviousStatus = previousCaseStatus,
                NewStatus = newCaseStatus,
                ChangedByAdminId = context.AdminUserId,
                Notes = $"Verification decision: {request.Action}.",
                CreatedAt = now
            });
            report.CaseStatus = newCaseStatus;
        }

        report.UpdatedAt = now;

        await visibilityService.RecomputeAsync(report, cancellationToken);

        await auditLogger.LogAsync(
            context.AdminUserId, "VerificationDecisionRecorded", nameof(IncidentReport), report.Id.ToString(),
            new { VerificationStatus = previousVerificationStatus.ToString(), CaseStatus = previousCaseStatus.ToString() },
            new { VerificationStatus = newVerificationStatus.ToString(), CaseStatus = newCaseStatus.ToString(), request.Reason },
            context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await reportService.GetByIdAsync(id, cancellationToken);
    }
}
