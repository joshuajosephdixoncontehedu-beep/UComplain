namespace CommunityIncidentReporting.Infrastructure.Email;

/// <summary>
/// Bound directly from flat environment variable names (RESEND_API_KEY, etc.) rather
/// than the "Section__Key" hierarchy convention used elsewhere in this project (Jwt__*,
/// WhatsApp__*) — Render's env var UI and the deployment docs for this integration name
/// these keys as flat strings, so DependencyInjection reads them via
/// configuration["RESEND_API_KEY"] instead of GetSection("Resend").Bind(...).
/// </summary>
public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string AppBaseUrl { get; set; } = string.Empty;
}
