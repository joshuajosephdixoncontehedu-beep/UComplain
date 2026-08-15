using CommunityIncidentReporting.Application.Features.Clarifications;
using CommunityIncidentReporting.Application.Features.Clarifications.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>
/// Reply-only surface for the clarification loop (api/mobile/clarifications). Listing a
/// report's clarification threads lives on ReportsController.GetClarifications instead —
/// this controller only takes a ClarificationRequestId directly, since a reply doesn't
/// need the report id in its route.
/// </summary>
public class ClarificationsController(IClarificationService clarificationService) : MobileControllerBase
{
    [HttpPost("{id:guid}/reply")]
    public async Task<ActionResult<ClarificationResponseDto>> Reply(
        Guid id, [FromBody] ReplyToClarificationRequest request, CancellationToken cancellationToken) =>
        Ok(await clarificationService.ReplyAsync(
            id, CurrentReporterId, request, RemoteIpAddress, UserAgentHeader, cancellationToken));
}
