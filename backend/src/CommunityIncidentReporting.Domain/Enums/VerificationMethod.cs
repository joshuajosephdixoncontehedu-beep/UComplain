namespace CommunityIncidentReporting.Domain.Enums;

/// <summary>How a VerificationEvent's decision was reached.</summary>
public enum VerificationMethod
{
    AdminReview = 0,
    AutomatedDuplicateCheck = 1,
    ReporterClarification = 2,
    AutomatedClarificationTimeout = 3
}
