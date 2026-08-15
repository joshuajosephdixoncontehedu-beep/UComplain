namespace CommunityIncidentReporting.Application.Features.Clarifications;

/// <summary>
/// The testable sweep logic ClarificationAutoCloseJob (a BackgroundService) delegates
/// to on each tick — separated out so tests can invoke a sweep deterministically instead
/// of waiting on a real timer.
/// </summary>
public interface IClarificationAutoCloseService
{
    /// <summary>Closes every report whose ClarificationRequest went unanswered past its DueAt. Returns how many were closed.</summary>
    Task<int> CloseOverdueAsync(CancellationToken cancellationToken);
}
