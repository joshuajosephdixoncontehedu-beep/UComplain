using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Features.Verification;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

[Route("api/admin/verification-queue")]
[Authorize(Policy = Policies.ReviewerOrAbove)]
public class VerificationQueueController(IVerificationService verificationService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VerificationQueueResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await verificationService.GetQueueAsync(cancellationToken));
}
