using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A report the reporter has started but not yet submitted — the mobile app's
/// multi-step wizard persists progress here after each step so a draft survives the app
/// being closed. Every field except Id/ReporterId/CreatedAt/UpdatedAt is nullable: the
/// wizard fills them in incrementally, and PATCH /reports/drafts/{id} always sends the
/// wizard's full current state (same "PATCH route, full-replace semantics" convention as
/// the admin side's UpdateReportRequest), not a partial diff.
///
/// SubmittedReportId is set exactly once, at submit — it makes a retried submit
/// idempotent (return the already-created report instead of erroring or duplicating)
/// without needing a separate client-supplied idempotency key, and it's why the draft is
/// kept around after submission rather than deleted.
/// </summary>
public class ReportDraft
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public Guid? CategoryId { get; set; }
    public IncidentCategory? Category { get; set; }

    public string? Description { get; set; }
    public DateTimeOffset? IncidentOccurredAt { get; set; }

    /// <summary>Recorded for context only — never becomes the submitted report's actual Priority. See CreateMobileReportRequest's doc comment for why.</summary>
    public IncidentPriority? InitialPrioritySignal { get; set; }

    public string? LocationDescription { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Landmark { get; set; }

    public Guid? SubmittedReportId { get; set; }
    public IncidentReport? SubmittedReport { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ReportDraftAttachment> Attachments { get; set; } = new List<ReportDraftAttachment>();
}
