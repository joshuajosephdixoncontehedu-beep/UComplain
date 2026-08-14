using System.Text.Json.Serialization;

namespace CommunityIncidentReporting.Infrastructure.WhatsApp;

// Mirrors only the fields this app reads from Meta's WhatsApp Cloud API webhook
// payload — not a full representation of everything Meta can send (statuses,
// non-text message types, etc. are read as null/absent and skipped).

public class WhatsAppWebhookPayload
{
    [JsonPropertyName("entry")]
    public List<WhatsAppEntry>? Entry { get; set; }
}

public class WhatsAppEntry
{
    [JsonPropertyName("changes")]
    public List<WhatsAppChange>? Changes { get; set; }
}

public class WhatsAppChange
{
    [JsonPropertyName("value")]
    public WhatsAppChangeValue? Value { get; set; }
}

public class WhatsAppChangeValue
{
    // Present on inbound messages; absent on delivery/read status-update events, which
    // this app has nothing to do with and silently skips.
    [JsonPropertyName("messages")]
    public List<WhatsAppMessage>? Messages { get; set; }
}

public class WhatsAppMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Unix seconds, as a string (Meta's own wire format).</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public WhatsAppTextBody? Text { get; set; }
}

public class WhatsAppTextBody
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
