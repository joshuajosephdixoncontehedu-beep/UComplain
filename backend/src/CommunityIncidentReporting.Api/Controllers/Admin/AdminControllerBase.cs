using System.Security.Claims;
using CommunityIncidentReporting.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public abstract class AdminControllerBase : ControllerBase
{
    protected Guid CurrentAdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected RequestContext BuildRequestContext() => new(
        CurrentAdminId,
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null);
}
