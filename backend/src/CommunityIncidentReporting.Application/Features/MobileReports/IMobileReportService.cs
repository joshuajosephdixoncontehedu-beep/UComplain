using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

namespace CommunityIncidentReporting.Application.Features.MobileReports;

/// <summary>
/// Mobile-app incident report submission and self-service viewing — unlike
/// IIncidentReportService (the admin operational queue, Verified-only), this exposes a
/// reporter's own reports regardless of verification status, since the reporter needs to
/// track a report from submission through verification.
/// </summary>
public interface IMobileReportService
{
    /// <summary>ReporterId always comes from the caller's JWT claim, never the request body.</summary>
    Task<MobileReportDetailDto> CreateAsync(
        CreateMobileReportRequest request, Guid reporterId, string? requestIp, string? userAgent,
        CancellationToken cancellationToken);

    Task<PagedResult<MobileReportListItemDto>> GetMyReportsAsync(
        Guid reporterId, PagedRequest query, CancellationToken cancellationToken);

    /// <summary>Throws NotFoundException if the report doesn't exist or isn't owned by reporterId — the two cases are indistinguishable to the caller.</summary>
    Task<MobileReportDetailDto> GetByIdAsync(Guid reportId, Guid reporterId, CancellationToken cancellationToken);
}
