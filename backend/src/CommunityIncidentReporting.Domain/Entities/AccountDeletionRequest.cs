using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A reporter's own request to delete their account — grace-period-cancellable.
/// AccountDeletionJob's sweep is what actually anonymizes the account once ScheduledForAt
/// passes; nothing happens to the account at request time beyond recording this row (a
/// reporter can keep using their session and cancel any time before then).
/// </summary>
public class AccountDeletionRequest
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public AccountDeletionStatus Status { get; set; } = AccountDeletionStatus.Pending;

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset ScheduledForAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
