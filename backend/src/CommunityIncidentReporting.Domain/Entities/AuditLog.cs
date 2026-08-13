namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// Immutable record of a state-changing action. Written by every mutating admin
/// endpoint — never edited or deleted through the application.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>Null only for actions attributable to the system itself, not an admin.</summary>
    public Guid? AdminUserId { get; set; }
    public AdminUser? AdminUser { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    public string? PreviousValueJson { get; set; }
    public string? NewValueJson { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
