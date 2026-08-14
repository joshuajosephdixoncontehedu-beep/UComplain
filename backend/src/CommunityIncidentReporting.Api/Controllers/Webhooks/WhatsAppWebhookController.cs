using System.Text;
using CommunityIncidentReporting.Application.Features.Webhooks;
using CommunityIncidentReporting.Infrastructure.WhatsApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Api.Controllers.Webhooks;

/// <summary>
/// Meta calls this directly — no admin JWT applies here (there's no user session to
/// present one), so the GET handshake's verify token and the POST's X-Hub-Signature-256
/// check are the only things authenticating these requests. See
/// docs/whatsapp-integration-plan.md for the full contract.
/// </summary>
[ApiController]
[Route("api/webhooks/whatsapp")]
public class WhatsAppWebhookController(IWhatsAppWebhookService webhookService, IOptions<WhatsAppOptions> options)
    : ControllerBase
{
    /// <summary>
    /// One-time subscription handshake Meta runs when you save the webhook URL in its
    /// dashboard. Must echo back the literal `hub.challenge` value as plain text.
    /// </summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expectedToken = options.Value.VerifyToken;
        if (mode == "subscribe" && !string.IsNullOrEmpty(expectedToken)
            && verifyToken == expectedToken && !string.IsNullOrEmpty(challenge))
        {
            return Content(challenge, "text/plain");
        }

        return StatusCode(StatusCodes.Status403Forbidden);
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        // Read the raw body manually rather than binding it via [FromBody]: the
        // signature must be computed over the exact bytes Meta sent, before any JSON
        // parsing, so there's no room for a deserialize-then-reserialize mismatch.
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (!webhookService.VerifySignature(rawBody, signature))
        {
            return Unauthorized();
        }

        await webhookService.HandleInboundAsync(rawBody, cancellationToken);

        // Meta expects a fast 200 regardless of what the payload contained (including
        // status updates and non-text messages this app doesn't act on) — anything else
        // gets treated as a delivery failure and retried.
        return Ok();
    }
}
