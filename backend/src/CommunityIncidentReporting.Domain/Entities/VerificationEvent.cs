using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// One verification attempt/decision on a report. A full history of these — not just
/// the current status — is what lets admins see who decided what, and when.
/// </summary>
public class VerificationEvent
{
    public Guid Id { get; set; }

    public Guid IncidentReportId { get; set; }
    public IncidentReport? IncidentReport { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public VerificationMethod VerificationMethod { get; set; }
    public VerificationDecisionResult Result { get; set; }
    public int AttemptNumber { get; set; }
    public string? Notes { get; set; }

    /// <summary>Null when the event was raised by an automated check rather than a human decision.</summary>
    public Guid? PerformedByAdminId { get; set; }
    public AdminUser? PerformedByAdmin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
