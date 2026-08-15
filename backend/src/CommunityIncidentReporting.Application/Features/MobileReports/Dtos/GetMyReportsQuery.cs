using CommunityIncidentReporting.Application.Common.Models;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// status omitted (or absent from the query string) returns every one of the caller's own
/// reports, matching the brief's "All" tab; Active/Resolved/Rejected filter to that
/// ReportStatusProjection bucket. NotListed (drafts/withdrawn) is reachable too — it's
/// just not one of the brief's default tabs, so no client is expected to request it.
/// </summary>
public record GetMyReportsQuery : PagedRequest
{
    public ReportListBucket? Status { get; init; }
}
