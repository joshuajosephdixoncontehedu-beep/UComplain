using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.PublicMap;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class ReportVisibilityService(AppDbContext db) : IReportVisibilityService
{
    public async Task RecomputeAsync(IncidentReport report, CancellationToken cancellationToken)
    {
        var showOnPublicMap = await db.ReporterPrivacySettings
            .Where(p => p.ReporterId == report.ReporterId)
            .Select(p => (bool?)p.ShowOnPublicMap)
            .FirstOrDefaultAsync(cancellationToken) ?? true;

        report.IsPubliclyVisible = ReportPublicVisibility.Compute(report.VerificationStatus, report.CaseStatus, showOnPublicMap);
    }
}
