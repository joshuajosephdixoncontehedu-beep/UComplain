namespace CommunityIncidentReporting.Domain.Enums;

public enum CaseStatus
{
    VerificationPending = 0,
    UnderReview = 1,
    Assigned = 2,
    InProgress = 3,
    Resolved = 4,
    Closed = 5,
    Rejected = 6,
    Duplicate = 7
}
