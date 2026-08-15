using CommunityIncidentReporting.Application.Features.PublicMap.Dtos;

namespace CommunityIncidentReporting.Application.Features.PublicMap;

/// <summary>Anonymous, unauthenticated — see PublicMapController. Never returns reporter identity, description, or exact location text.</summary>
public interface IPublicMapService
{
    /// <summary>
    /// radiusM is clamped to a sane range server-side regardless of what's requested.
    /// categoryIds, if supplied, restricts results to those categories only.
    /// </summary>
    Task<IReadOnlyList<PublicIncidentDto>> GetNearbyAsync(
        double lat, double lng, double? radiusM, Guid[]? categoryIds, CancellationToken cancellationToken);
}
