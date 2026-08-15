namespace CommunityIncidentReporting.Domain.Enums;

/// <summary>
/// The value is modeled as an enum (not hardcoded everywhere) so additional intake
/// channels can be added later without a schema migration touching every consumer —
/// stored as text (see AppDbContext.ConfigureConventions), so adding MobileApp here
/// required no migration of its own.
/// </summary>
public enum SourceChannel
{
    WhatsApp = 0,
    MobileApp = 1
}
