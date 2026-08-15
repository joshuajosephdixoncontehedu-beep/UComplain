using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.ReporterAccount;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Compliance;

public class AccountDeletionProcessorService(
    AppDbContext db, IReporterAnonymizationService anonymizationService, IAuditLogger auditLogger)
    : IAccountDeletionProcessorService
{
    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var due = await db.AccountDeletionRequests
            .Include(d => d.Reporter)
            .Where(d => d.Status == AccountDeletionStatus.Pending && d.ScheduledForAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var request in due)
        {
            await anonymizationService.AnonymizeAsync(request.Reporter!, cancellationToken);

            request.Status = AccountDeletionStatus.Completed;
            request.CompletedAt = now;

            await auditLogger.LogAsync(
                adminUserId: null, "ReporterAccountDeletionExecuted", nameof(Reporter), request.ReporterId.ToString(),
                previousValue: null, newValue: new { request.Id }, ipAddress: null, userAgent: null, cancellationToken);
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }
}
