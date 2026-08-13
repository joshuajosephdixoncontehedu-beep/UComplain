using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A community member who reports incidents via WhatsApp. We never store the raw
/// WhatsApp number — only a one-way hash (for de-duplication/lookup) and a masked
/// display reference (e.g. "+232 76 ***  123") for admin UI.
/// </summary>
public class Reporter
{
    public Guid Id { get; set; }
    public string WhatsAppNumberHash { get; set; } = string.Empty;
    public string MaskedContactReference { get; set; } = string.Empty;
    public VerificationStatus VerificationStatus { get; set; }
    public DateTimeOffset? ConsentAt { get; set; }
    public bool IsRestricted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();
    public ICollection<VerificationEvent> VerificationEvents { get; set; } = new List<VerificationEvent>();
}
