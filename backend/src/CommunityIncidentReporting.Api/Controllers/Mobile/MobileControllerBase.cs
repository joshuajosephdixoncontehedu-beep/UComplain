using System.Security.Claims;
using CommunityIncidentReporting.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>
/// Base for reporter-authenticated mobile endpoints — mirrors AdminControllerBase, but
/// requires the "ReporterScheme" JWT scheme + RequireReporter policy instead of the
/// default admin scheme, so an admin token can never satisfy [Authorize] here (and vice
/// versa; see Program.cs's two independently-keyed JwtBearer registrations).
/// </summary>
[ApiController]
[Route("api/mobile/[controller]")]
[Authorize(Policy = Policies.RequireReporter)]
public abstract class MobileControllerBase : ControllerBase
{
    protected Guid CurrentReporterId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected string? RemoteIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    protected string? UserAgentHeader =>
        Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;
}
