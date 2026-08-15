using System.Text;
using CommunityIncidentReporting.Application.Features.Analytics;
using CommunityIncidentReporting.Application.Features.Analytics.Dtos;
using CommunityIncidentReporting.Application.Features.Dashboard;
using CommunityIncidentReporting.Application.Features.Dashboard.Dtos;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class AnalyticsService(AppDbContext db, IDashboardService dashboardService) : IAnalyticsService
{
    private static readonly CaseStatus[] OpenStatuses =
        [CaseStatus.VerificationPending, CaseStatus.UnderReview, CaseStatus.Assigned, CaseStatus.InProgress];
    private static readonly CaseStatus[] ResolvedStatuses = [CaseStatus.Resolved, CaseStatus.Closed];

    public async Task<AnalyticsResponse> GetAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var dashboard = await dashboardService.GetAsync(from, to, cancellationToken);

        var fromUtc = new DateTimeOffset(dashboard.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(dashboard.To.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var workload = await GetAssignmentWorkloadAsync(fromUtc, toUtc, cancellationToken);
        var resolutionByCategory = await GetResolutionTimeByCategoryAsync(fromUtc, toUtc, cancellationToken);
        var resolvedBySourceChannel = await GetResolvedBySourceChannelAsync(fromUtc, toUtc, cancellationToken);
        var verificationOutcomesBySourceChannel = await GetVerificationOutcomesBySourceChannelAsync(fromUtc, toUtc, cancellationToken);

        return new AnalyticsResponse(
            dashboard.From, dashboard.To, dashboard.Current, dashboard.ReportVolumeOverTime,
            dashboard.CategoryDistribution, dashboard.StatusDistribution, dashboard.VerificationOutcomeDistribution,
            workload, resolutionByCategory, resolvedBySourceChannel, verificationOutcomesBySourceChannel);
    }

    public async Task<byte[]> ExportCsvAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from ?? toDate.AddDays(-29);
        var fromUtc = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var rows = await db.IncidentReports
            .Where(r => r.CreatedAt >= fromUtc && r.CreatedAt <= toUtc)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new
            {
                r.CaseReference,
                CategoryName = r.Category!.Name,
                r.Priority,
                r.VerificationStatus,
                r.CaseStatus,
                r.LocationDescription,
                r.CreatedAt,
                r.ClosedAt
            })
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("CaseReference,Category,Priority,VerificationStatus,CaseStatus,Location,CreatedAt,ClosedAt");
        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(',',
                CsvField(row.CaseReference), CsvField(row.CategoryName), row.Priority, row.VerificationStatus,
                row.CaseStatus, CsvField(row.LocationDescription), row.CreatedAt.ToString("O"),
                row.ClosedAt?.ToString("O") ?? ""));
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static string CsvField(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private async Task<IReadOnlyList<AssignmentWorkloadDto>> GetAssignmentWorkloadAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var admins = await db.AdminUsers
            .Where(a => a.IsActive && (a.Role == AdminRole.IncidentManager || a.Role == AdminRole.Reviewer))
            .Select(a => new { a.Id, a.FullName })
            .ToListAsync(cancellationToken);

        // GroupBy is applied client-side (see DashboardService's similar notes) — safer
        // and consistent, even though these particular groupings are on a plain Guid?
        // column rather than a converted enum or joined navigation.
        var openAdminIds = await db.IncidentReports
            .Where(r => r.AssignedAdminId != null && OpenStatuses.Contains(r.CaseStatus))
            .Select(r => r.AssignedAdminId!.Value)
            .ToListAsync(cancellationToken);
        var openCounts = openAdminIds.GroupBy(id => id).Select(g => new { AdminId = g.Key, Count = g.Count() }).ToList();

        var resolvedAdminIds = await db.IncidentReports
            .Where(r => r.AssignedAdminId != null && ResolvedStatuses.Contains(r.CaseStatus)
                && r.UpdatedAt >= fromUtc && r.UpdatedAt <= toUtc)
            .Select(r => r.AssignedAdminId!.Value)
            .ToListAsync(cancellationToken);
        var resolvedCounts = resolvedAdminIds.GroupBy(id => id).Select(g => new { AdminId = g.Key, Count = g.Count() }).ToList();

        return admins.Select(a => new AssignmentWorkloadDto(
            a.Id, a.FullName,
            openCounts.FirstOrDefault(c => c.AdminId == a.Id)?.Count ?? 0,
            resolvedCounts.FirstOrDefault(c => c.AdminId == a.Id)?.Count ?? 0)).ToList();
    }

    private async Task<IReadOnlyList<CategoryResponseTimeDto>> GetResolutionTimeByCategoryAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var raw = await db.StatusHistories
            .Where(s => s.NewStatus == CaseStatus.Resolved && s.CreatedAt >= fromUtc && s.CreatedAt <= toUtc)
            .Select(s => new
            {
                s.CreatedAt,
                ReportCreatedAt = s.IncidentReport!.CreatedAt,
                CategoryName = s.IncidentReport!.Category!.Name
            })
            .ToListAsync(cancellationToken);

        return raw.GroupBy(x => x.CategoryName)
            .Select(g => new CategoryResponseTimeDto(g.Key, g.Average(x => (x.CreatedAt - x.ReportCreatedAt).TotalHours)))
            .OrderBy(x => x.CategoryName)
            .ToList();
    }

    private async Task<IReadOnlyList<NamedCountDto>> GetResolvedBySourceChannelAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var resolvedInRange = db.IncidentReports.Where(r =>
            ResolvedStatuses.Contains(r.CaseStatus) && r.UpdatedAt >= fromUtc && r.UpdatedAt <= toUtc);
        return await DashboardService.GetSourceChannelBreakdownAsync(resolvedInRange, cancellationToken);
    }

    private async Task<IReadOnlyList<SourceChannelVerificationOutcomeDto>> GetVerificationOutcomesBySourceChannelAsync(
        DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var raw = await db.VerificationEvents
            .Where(v => v.CreatedAt >= fromUtc && v.CreatedAt <= toUtc)
            .Select(v => new { v.Result, SourceChannel = v.IncidentReport!.SourceChannel })
            .ToListAsync(cancellationToken);

        return raw.GroupBy(x => new { x.SourceChannel, x.Result })
            .Select(g => new SourceChannelVerificationOutcomeDto(g.Key.SourceChannel, g.Key.Result, g.Count()))
            .OrderBy(x => x.SourceChannel).ThenBy(x => x.Result)
            .ToList();
    }
}
