using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

public class StatusHistory
{
    public Guid Id { get; set; }

    public Guid IncidentReportId { get; set; }
    public IncidentReport? IncidentReport { get; set; }

    public CaseStatus PreviousStatus { get; set; }
    public CaseStatus NewStatus { get; set; }

    public Guid ChangedByAdminId { get; set; }
    public AdminUser? ChangedByAdmin { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
