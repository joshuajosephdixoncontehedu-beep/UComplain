namespace CommunityIncidentReporting.Application.Features.ReporterAccount;

/// <summary>The sweep behind grace-period-expired account-deletion requests — see AccountDeletionJob.</summary>
public interface IAccountDeletionProcessorService
{
    /// <summary>Anonymizes every reporter whose Pending request's ScheduledForAt has passed. Returns how many were processed.</summary>
    Task<int> ProcessDueAsync(CancellationToken cancellationToken);
}
