namespace CommunityIncidentReporting.Domain.Enums;

/// <summary>
/// A report only enters the operational report queue once this reaches Verified.
/// All other values keep the report in the verification queue, separated from
/// active operational cases.
/// </summary>
public enum VerificationStatus
{
    Pending = 0,
    Verified = 1,
    NeedsClarification = 2,
    SuspectedDuplicate = 3,
    FlaggedAbuse = 4,
    Rejected = 5
}
