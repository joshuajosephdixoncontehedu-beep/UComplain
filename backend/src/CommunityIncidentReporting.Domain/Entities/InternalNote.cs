namespace CommunityIncidentReporting.Domain.Entities;

public class InternalNote
{
    public Guid Id { get; set; }

    public Guid IncidentReportId { get; set; }
    public IncidentReport? IncidentReport { get; set; }

    public string Content { get; set; } = string.Empty;

    public Guid CreatedByAdminId { get; set; }
    public AdminUser? CreatedByAdmin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
