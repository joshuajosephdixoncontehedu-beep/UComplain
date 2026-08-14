namespace CommunityIncidentReporting.Application.Features.Webhooks;

public interface IWhatsAppWebhookService
{
    /// <summary>
    /// Verifies the X-Hub-Signature-256 header against the raw request body using the
    /// configured App Secret. Must be called before HandleInboundAsync — there is no
    /// other authentication on this endpoint (it can't require an admin JWT; Meta is
    /// the caller), so this is the only thing standing between it and the public
    /// internet. Returns false (fail closed) if the App Secret isn't configured at all.
    /// </summary>
    bool VerifySignature(string rawBody, string? signatureHeaderValue);

    /// <summary>
    /// Parses an already-signature-verified webhook payload and, for every inbound text
    /// message it contains, resolves or creates the Reporter and creates a new Pending
    /// IncidentReport from it. Non-text messages and non-message events (status
    /// updates) are logged and skipped, not rejected — Meta still expects a 200 either
    /// way.
    /// </summary>
    Task HandleInboundAsync(string rawBody, CancellationToken cancellationToken);
}
