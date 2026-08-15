namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// One round of an admin asking a reporter for more information — created whenever
/// VerificationService.DecideAsync records a RequestClarification decision. A report can
/// accumulate several of these over its lifetime (each new RequestClarification decision
/// adds another row). ResolvedAt is set on the reporter's first reply; if it's still null
/// past DueAt, ClarificationAutoCloseService closes the report.
/// </summary>
public class ClarificationRequest
{
    public Guid Id { get; set; }

    public Guid IncidentReportId { get; set; }
    public IncidentReport? IncidentReport { get; set; }

    public Guid RequestedByAdminId { get; set; }
    public AdminUser? RequestedByAdmin { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? AutoClosedAt { get; set; }

    public ICollection<ClarificationResponse> Responses { get; set; } = new List<ClarificationResponse>();
}
