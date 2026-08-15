namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// Which tab of the mobile app's report list (§3.4 of the product brief) a report
/// belongs in. Never persisted — always derived fresh by ReportStatusProjection.
/// </summary>
public enum ReportListBucket
{
    Active,
    Resolved,
    Rejected,
    /// <summary>Draft or Withdrawn — exists, but not shown in any default list tab.</summary>
    NotListed
}
