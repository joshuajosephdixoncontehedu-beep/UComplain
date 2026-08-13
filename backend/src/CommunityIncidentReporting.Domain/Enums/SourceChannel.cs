namespace CommunityIncidentReporting.Domain.Enums;

/// <summary>
/// Only WhatsApp exists today. The value is modeled as an enum (not hardcoded
/// everywhere) so additional intake channels can be added later without a
/// schema migration touching every consumer.
/// </summary>
public enum SourceChannel
{
    WhatsApp = 0
}
