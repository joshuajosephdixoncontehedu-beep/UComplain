using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

public class IncidentCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Mobile-app catalogue display fields — nullable because existing categories predate
    // them; an admin fills these in via the Categories page before a category is meant to
    // appear in the mobile app's category picker.
    public string? Slug { get; set; }
    public string? IconKey { get; set; }
    public string? ColourToken { get; set; }

    public IncidentPriority DefaultPriority { get; set; }
    public int SlaHours { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();
}
