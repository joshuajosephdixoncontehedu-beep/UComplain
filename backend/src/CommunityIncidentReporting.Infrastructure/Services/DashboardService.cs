using CommunityIncidentReporting.Application.Features.Dashboard;
using CommunityIncidentReporting.Application.Features.Dashboard.Dtos;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class DashboardService(AppDbContext db) : IDashboardService
{
    private static readonly CaseStatus[] InProgressStatuses = [CaseStatus.Assigned, CaseStatus.InProgress];
    private static readonly CaseStatus[] ResolvedStatuses = [CaseStatus.Resolved, CaseStatus.Closed];
    private static readonly VerificationStatus[] RejectedDuplicateFlaggedStatuses =
        [VerificationStatus.Rejected, VerificationStatus.SuspectedDuplicate, VerificationStatus.FlaggedAbuse];

    public async Task<DashboardResponse> GetAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? toDate.AddDays(-29);

        var fromUtc = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var rangeLength = toUtc - fromUtc;
        var previousToUtc = fromUtc.AddTicks(-1);
        var previousFromUtc = previousToUtc - rangeLength;

        var current = await ComputeMetricsAsync(fromUtc, toUtc, cancellationToken);
        var previous = await ComputeMetricsAsync(previousFromUtc, previousToUtc, cancellationToken);

        var reportVolume = await GetReportVolumeAsync(fromDate, toDate, fromUtc, toUtc, cancellationToken);
        var categoryDistribution = await GetCategoryDistributionAsync(fromUtc, toUtc, cancellationToken);
        var statusDistribution = await GetStatusDistributionAsync(fromUtc, toUtc, cancellationToken);
        var verificationOutcomeDistribution = await GetVerificationOutcomeDistributionAsync(fromUtc, toUtc, cancellationToken);
        var topHotspots = await GetTopHotspotsAsync(fromUtc, toUtc, cancellationToken);
        var priorityReports = await GetPriorityReportsAsync(cancellationToken);
        var recentActivity = await GetRecentActivityAsync(cancellationToken);
        var queueSnapshot = await GetVerificationQueueSnapshotAsync(cancellationToken);

        return new DashboardResponse(
            fromDate, toDate, current, previous, reportVolume, categoryDistribution, statusDistribution,
            verificationOutcomeDistribution, topHotspots, priorityReports, recentActivity, queueSnapshot);
    }

    private async Task<DashboardMetricsDto> ComputeMetricsAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var reportsInRange = db.IncidentReports.Where(r => r.CreatedAt >= fromUtc && r.CreatedAt <= toUtc);

        var total = await reportsInRange.CountAsync(cancellationToken);
        var awaitingVerification = await reportsInRange
            .CountAsync(r => r.VerificationStatus == VerificationStatus.Pending, cancellationToken);
        var verifiedAwaitingReview = await reportsInRange.CountAsync(
            r => r.VerificationStatus == VerificationStatus.Verified && r.CaseStatus == CaseStatus.UnderReview,
            cancellationToken);
        var inProgress = await reportsInRange.CountAsync(r => InProgressStatuses.Contains(r.CaseStatus), cancellationToken);
        var resolved = await reportsInRange.CountAsync(r => ResolvedStatuses.Contains(r.CaseStatus), cancellationToken);
        var rejectedDuplicateFlagged = await reportsInRange
            .CountAsync(r => RejectedDuplicateFlaggedStatuses.Contains(r.VerificationStatus), cancellationToken);

        var verificationDurations = await db.VerificationEvents
            .Where(v => v.Result == VerificationDecisionResult.Approved && v.CreatedAt >= fromUtc && v.CreatedAt <= toUtc)
            .Select(v => new { v.CreatedAt, ReportCreatedAt = v.IncidentReport!.CreatedAt })
            .ToListAsync(cancellationToken);
        var averageVerificationTimeHours = verificationDurations.Count == 0
            ? (double?)null
            : verificationDurations.Average(x => (x.CreatedAt - x.ReportCreatedAt).TotalHours);

        var resolutionDurations = await db.StatusHistories
            .Where(s => s.NewStatus == CaseStatus.Resolved && s.CreatedAt >= fromUtc && s.CreatedAt <= toUtc)
            .Select(s => new { s.CreatedAt, ReportCreatedAt = s.IncidentReport!.CreatedAt })
            .ToListAsync(cancellationToken);
        var averageResolutionTimeHours = resolutionDurations.Count == 0
            ? (double?)null
            : resolutionDurations.Average(x => (x.CreatedAt - x.ReportCreatedAt).TotalHours);

        var reportsBySourceChannel = await GetSourceChannelBreakdownAsync(reportsInRange, cancellationToken);

        return new DashboardMetricsDto(
            total, awaitingVerification, verifiedAwaitingReview, inProgress, resolved, rejectedDuplicateFlagged,
            averageVerificationTimeHours, averageResolutionTimeHours, reportsBySourceChannel);
    }

    /// <summary>
    /// Client-side grouping — same reasoning as the other distributions in this file:
    /// EF Core's GroupBy translation over an enum-as-string column is unreliable.
    /// </summary>
    internal static async Task<IReadOnlyList<NamedCountDto>> GetSourceChannelBreakdownAsync(
        IQueryable<IncidentReport> reports, CancellationToken cancellationToken)
    {
        var raw = await reports.Select(r => r.SourceChannel).ToListAsync(cancellationToken);
        return raw.GroupBy(s => s)
            .Select(g => new NamedCountDto(g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private async Task<IReadOnlyList<TimeSeriesPointDto>> GetReportVolumeAsync(
        DateOnly fromDate, DateOnly toDate, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var raw = await db.IncidentReports
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt <= toUtc)
            .Select(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var byDate = raw.GroupBy(c => DateOnly.FromDateTime(c.UtcDateTime))
            .ToDictionary(g => g.Key, g => g.Count());

        var points = new List<TimeSeriesPointDto>();
        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            points.Add(new TimeSeriesPointDto(day, byDate.GetValueOrDefault(day)));
        }

        return points;
    }

    private async Task<IReadOnlyList<NamedCountDto>> GetCategoryDistributionAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        // GroupBy translation over a joined navigation (Category.Name) is unreliable
        // in EF Core/Npgsql — see VerificationService's note on the same issue. Fetch
        // the raw category names and group client-side instead.
        var raw = await db.IncidentReports
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt <= toUtc)
            .Select(r => r.Category!.Name)
            .ToListAsync(cancellationToken);

        return raw.GroupBy(name => name)
            .Select(g => new NamedCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private async Task<IReadOnlyList<NamedCountDto>> GetStatusDistributionAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var raw = await db.IncidentReports
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt <= toUtc)
            .Select(r => r.CaseStatus)
            .ToListAsync(cancellationToken);

        return raw.GroupBy(s => s)
            .Select(g => new NamedCountDto(g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private async Task<IReadOnlyList<NamedCountDto>> GetVerificationOutcomeDistributionAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var raw = await db.VerificationEvents
            .Where(v => v.CreatedAt >= fromUtc && v.CreatedAt <= toUtc)
            .Select(v => v.Result)
            .ToListAsync(cancellationToken);

        return raw.GroupBy(r => r)
            .Select(g => new NamedCountDto(g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private async Task<IReadOnlyList<NamedCountDto>> GetTopHotspotsAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var raw = await db.IncidentReports
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt <= toUtc)
            .Select(r => r.LocationDescription)
            .ToListAsync(cancellationToken);

        return raw.GroupBy(location => location)
            .Select(g => new NamedCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();
    }

    private async Task<IReadOnlyList<PriorityReportItemDto>> GetPriorityReportsAsync(CancellationToken cancellationToken) =>
        await db.IncidentReports
            .Where(r => r.VerificationStatus == VerificationStatus.Verified
                && (r.Priority == IncidentPriority.Critical || r.Priority == IncidentPriority.High)
                && r.CaseStatus != CaseStatus.Resolved && r.CaseStatus != CaseStatus.Closed)
            .OrderBy(r => r.CreatedAt)
            .Take(10)
            .Select(r => new PriorityReportItemDto(
                r.Id, r.CaseReference, r.Category!.Name, r.LocationDescription, r.Priority, r.CaseStatus,
                r.AssignedAdmin != null ? r.AssignedAdmin.FullName : null, r.CreatedAt))
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<AuditLogEntryDto>> GetRecentActivityAsync(CancellationToken cancellationToken) =>
        await db.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(20)
            .Select(l => new AuditLogEntryDto(
                l.Id, l.AdminUserId, l.AdminUser != null ? l.AdminUser.FullName : null, l.Action,
                l.PreviousValueJson, l.NewValueJson, l.CreatedAt))
            .ToListAsync(cancellationToken);

    private async Task<VerificationQueueSnapshotDto> GetVerificationQueueSnapshotAsync(CancellationToken cancellationToken)
    {
        var raw = await db.IncidentReports
            .Where(r => r.VerificationStatus != VerificationStatus.Verified)
            .Select(r => r.VerificationStatus)
            .ToListAsync(cancellationToken);

        var counts = raw.GroupBy(status => status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();

        int CountFor(VerificationStatus status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        return new VerificationQueueSnapshotDto(
            CountFor(VerificationStatus.Pending),
            CountFor(VerificationStatus.NeedsClarification),
            CountFor(VerificationStatus.SuspectedDuplicate),
            CountFor(VerificationStatus.FlaggedAbuse),
            CountFor(VerificationStatus.Rejected));
    }
}
