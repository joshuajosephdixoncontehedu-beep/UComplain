namespace CommunityIncidentReporting.Domain.Entities;

public class ReportAssignment
{
    public Guid Id { get; set; }

    public Guid IncidentReportId { get; set; }
    public IncidentReport? IncidentReport { get; set; }

    public Guid AdminUserId { get; set; }
    public AdminUser? AdminUser { get; set; }

    public Guid AssignedByAdminId { get; set; }
    public AdminUser? AssignedByAdmin { get; set; }

    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? UnassignedAt { get; set; }
}
