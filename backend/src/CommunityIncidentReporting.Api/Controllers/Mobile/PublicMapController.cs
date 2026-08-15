using CommunityIncidentReporting.Application.Features.PublicMap;
using CommunityIncidentReporting.Application.Features.PublicMap.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>
/// The one entirely anonymous surface under api/mobile/... — no reporter token, unlike
/// every MobileControllerBase-derived controller. See IPublicMapService for what's
/// deliberately never returned (reporter identity, description, exact location text).
/// </summary>
[ApiController]
[Route("api/mobile/public/incidents")]
[AllowAnonymous]
public class PublicMapController(IPublicMapService publicMapService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublicIncidentDto>>> GetNearby(
        [FromQuery] double lat, [FromQuery] double lng, [FromQuery] double? radiusM,
        [FromQuery(Name = "categories")] Guid[]? categories, CancellationToken cancellationToken) =>
        Ok(await publicMapService.GetNearbyAsync(lat, lng, radiusM, categories, cancellationToken));
}
