namespace CommunityIncidentReporting.Application.Features.ReporterAccount;

/// <summary>The sweep enforcing SystemSettings.ReporterDataRetentionMonths — see ReporterRetentionPurgeJob.</summary>
public interface IReporterRetentionPurgeService
{
    /// <summary>Anonymizes every reporter whose last activity is older than the configured retention window. Returns how many were processed.</summary>
    Task<int> PurgeInactiveAsync(CancellationToken cancellationToken);
}
