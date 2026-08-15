namespace CommunityIncidentReporting.Domain.Enums;

/// <summary>What kind of event a Notification represents — drives the mobile client's icon/grouping, never behavior server-side.</summary>
public enum NotificationType
{
    ClarificationRequested,
    ReportVerified,
    AssignmentMade,
    WorkStarted,
    ReportResolved,
    ReportRejected,
    ReportClosedDuplicate,
    ReportAutoClosed
}
