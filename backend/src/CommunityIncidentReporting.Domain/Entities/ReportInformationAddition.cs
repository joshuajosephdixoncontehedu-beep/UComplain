namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A supplementary message a reporter adds to their own report after submission (e.g.
/// "it's gotten worse", "here's another angle") — distinct from Phase 5's
/// ClarificationResponse, which replies to an admin's specific request. AttachmentId
/// optionally points at an already-uploaded IncidentMediaAttachment on the same report;
/// this never accepts a new file upload directly (see IMobileReportService.AddInformationAsync).
/// </summary>
public class ReportInformationAddition
{
    public Guid Id { get; set; }

    public Guid IncidentReportId { get; set; }
    public IncidentReport? IncidentReport { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? AttachmentId { get; set; }
    public IncidentMediaAttachment? Attachment { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
