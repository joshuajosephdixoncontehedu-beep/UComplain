namespace CommunityIncidentReporting.Infrastructure.WhatsApp;

public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>Meta App Secret — verifies X-Hub-Signature-256 on inbound webhook calls.</summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>Arbitrary secret you choose; must match what's entered in Meta's webhook setup form.</summary>
    public string VerifyToken { get; set; } = string.Empty;

    /// <summary>Used to send outbound replies via the Cloud API. Missing/blank just skips sending one.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>From WhatsApp -> API Setup in the Meta Developer Console.</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "v21.0";

    /// <summary>
    /// HMAC key for hashing inbound WhatsApp numbers before they're ever written to
    /// Reporter.WhatsAppNumberHash. Deliberately not the same key as anything else —
    /// rotating it would strand every existing reporter's lookup, so generate it once
    /// (e.g. `openssl rand -base64 32`) and treat it like any other long-lived secret.
    /// A plain unsalted SHA-256 would be reversible by brute force here (phone numbers
    /// are a small keyspace, unlike a real cryptographic secret), which is exactly what
    /// Reporter's "we never store the raw number" comment is meant to prevent.
    /// </summary>
    public string NumberHashKey { get; set; } = string.Empty;
}
