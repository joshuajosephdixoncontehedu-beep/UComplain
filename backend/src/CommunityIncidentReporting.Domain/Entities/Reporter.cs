using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A community member who reports incidents, either via WhatsApp or via the mobile app.
/// WhatsApp-originated reporters never have a raw WhatsApp number stored — only a
/// one-way hash (for de-duplication/lookup) and a masked display reference (e.g.
/// "+232 76 *** 123"). Mobile-app reporters instead authenticate with an email/password
/// account and are identified by <see cref="Email"/>; <see cref="WhatsAppNumberHash"/>/
/// <see cref="MaskedContactReference"/> are left empty for them. The two identity shapes
/// share this single table because both still map 1:1 onto the same IncidentReport
/// ownership/verification pipeline.
/// </summary>
public class Reporter
{
    public Guid Id { get; set; }
    public string WhatsAppNumberHash { get; set; } = string.Empty;
    public string MaskedContactReference { get; set; } = string.Empty;

    // --- Mobile-app account fields (null/empty for WhatsApp-only reporters) ---
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }

    /// <summary>
    /// Raw phone number captured during mobile registration. Unlike
    /// <see cref="WhatsAppNumberHash"/>, this is stored as provided — it's contact info
    /// the reporter directly and knowingly supplies for their own account, analogous to
    /// <see cref="Email"/>, not an anonymized lookup key.
    /// </summary>
    public string? PhoneNumber { get; set; }

    public string? PasswordHash { get; set; }
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RestrictionReason { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public VerificationStatus VerificationStatus { get; set; }
    public DateTimeOffset? ConsentAt { get; set; }
    public bool IsRestricted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();
    public ICollection<VerificationEvent> VerificationEvents { get; set; } = new List<VerificationEvent>();
    public ICollection<ReporterRefreshToken> RefreshTokens { get; set; } = new List<ReporterRefreshToken>();
}
