using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A reporter-facing notification, persisted only — no push send in this phase (see
/// INotificationService). ReportId is nullable because a future notification type might
/// not be tied to a specific report.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public Guid? ReportId { get; set; }
    public IncidentReport? Report { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
