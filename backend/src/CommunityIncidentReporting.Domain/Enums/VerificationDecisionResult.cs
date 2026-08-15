namespace CommunityIncidentReporting.Domain.Enums;

/// <summary>The outcome recorded on a single VerificationEvent attempt.</summary>
public enum VerificationDecisionResult
{
    Approved = 0,
    Rejected = 1,
    ClarificationRequested = 2,
    MarkedDuplicate = 3,
    Escalated = 4,

    /// <summary>System-raised, not a human decision — see ClarificationAutoCloseService.</summary>
    AutoClosed = 5
}
