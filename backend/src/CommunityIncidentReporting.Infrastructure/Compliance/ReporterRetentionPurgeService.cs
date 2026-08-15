using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.ReporterAccount;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Compliance;

/// <summary>
/// Enforces the already-existing SystemSettings.ReporterDataRetentionMonths field
/// (configured today via the admin Settings page, never enforced before this phase).
/// "Last activity" is LastLoginAt for a reporter who has ever logged in, else their most
/// recent report's CreatedAt, else their own CreatedAt if they've never reported anything
/// either — covers both mobile-app and WhatsApp-only reporters (who never log in at all).
/// Reuses the exact same anonymization as a reporter's own AccountDeletionRequest — see
/// IReporterAnonymizationService's doc comment for why the two paths share it.
/// </summary>
public class ReporterRetentionPurgeService(AppDbContext db, IReporterAnonymizationService anonymizationService, IAuditLogger auditLogger)
    : IReporterRetentionPurgeService
{
    // Matches SettingsService's own get-or-create default, for the (practically
    // unreachable, since SettingsService always creates the row on first admin read)
    // case this runs before SystemSettings has ever been read.
    private const int DefaultRetentionMonths = 24;

    public async Task<int> PurgeInactiveAsync(CancellationToken cancellationToken)
    {
        var retentionMonths = await db.SystemSettings
            .Select(s => (int?)s.ReporterDataRetentionMonths)
            .FirstOrDefaultAsync(cancellationToken) ?? DefaultRetentionMonths;

        var cutoff = DateTimeOffset.UtcNow.AddMonths(-retentionMonths);

        var candidates = await db.Reporters
            .Where(r => r.AnonymizedAt == null)
            .Select(r => new
            {
                Reporter = r,
                LastActivity = r.LastLoginAt ?? (r.IncidentReports.Any() ? r.IncidentReports.Max(x => x.CreatedAt) : r.CreatedAt)
            })
            .Where(x => x.LastActivity < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            await anonymizationService.AnonymizeAsync(candidate.Reporter, cancellationToken);

            await auditLogger.LogAsync(
                adminUserId: null, "ReporterDataRetentionPurged", nameof(Reporter), candidate.Reporter.Id.ToString(),
                previousValue: null, newValue: new { RetentionMonths = retentionMonths, candidate.LastActivity },
                ipAddress: null, userAgent: null, cancellationToken);
        }

        if (candidates.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return candidates.Count;
    }
}
