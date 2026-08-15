namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

/// <summary>
/// RememberMe defaults true — a long-lived refresh token, matching the behavior every
/// existing caller already gets — so a client that hasn't been updated to send this
/// field yet is unaffected.
/// </summary>
public record ReporterLoginRequest(string Email, string Password, bool RememberMe = true);
