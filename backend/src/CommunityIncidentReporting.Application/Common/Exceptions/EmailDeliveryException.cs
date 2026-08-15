namespace CommunityIncidentReporting.Application.Common.Exceptions;

/// <summary>
/// The configured email provider (Resend) failed to accept or send a message. Maps to
/// HTTP 502 Bad Gateway — the request itself was valid, but a downstream dependency
/// failed. The message passed here must always be safe to show a client (never include
/// provider response bodies, API keys, or OTP codes).
/// </summary>
public class EmailDeliveryException(string message) : Exception(message);
