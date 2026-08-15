namespace CommunityIncidentReporting.Infrastructure.Storage;

/// <summary>
/// Bound from flat environment variable names (SUPABASE_URL, etc.), matching
/// ResendOptions' convention — see that class for why. Not fail-fast validated at
/// startup (like WhatsAppOptions, unlike JwtOptions/ReporterJwtOptions) — an unconfigured
/// deployment still boots; only media upload/access endpoints fail until these are set.
/// </summary>
public class SupabaseStorageOptions
{
    public string Url { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string StorageBucket { get; set; } = string.Empty;
}
