using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

public class IncidentReport
{
    public Guid Id { get; set; }

    /// <summary>
    /// Server-generated, human-readable reference, e.g. "CIRS-2026-000001" — left at
    /// its CLR default (null) rather than "", because EF Core's ValueGeneratedOnAdd
    /// only omits a column from the INSERT (deferring to the Postgres-side default) when
    /// the property still holds the CLR type's true default value. An explicit ""
    /// looks like an app-supplied value and defeats that detection, sending an empty
    /// string to every row instead of letting nextval() generate a unique reference.
    /// </summary>
    public string CaseReference { get; set; } = null!;

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public Guid CategoryId { get; set; }
    public IncidentCategory? Category { get; set; }

    public SourceChannel SourceChannel { get; set; } = SourceChannel.WhatsApp;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset IncidentOccurredAt { get; set; }
    public string LocationDescription { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? MediaReference { get; set; }

    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public CaseStatus CaseStatus { get; set; } = CaseStatus.VerificationPending;
    public IncidentPriority Priority { get; set; }

    public Guid? AssignedAdminId { get; set; }
    public AdminUser? AssignedAdmin { get; set; }

    public string? ResolutionSummary { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public ICollection<VerificationEvent> VerificationEvents { get; set; } = new List<VerificationEvent>();
    public ICollection<ReportAssignment> ReportAssignments { get; set; } = new List<ReportAssignment>();
    public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
    public ICollection<InternalNote> InternalNotes { get; set; } = new List<InternalNote>();
}
