namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

/// <summary>The consent screen grants several types at once (Location, Camera, Notifications, DataProcessing) in a single submission.</summary>
public record RecordConsentRequest(IReadOnlyList<ConsentGrantItem> Consents);
