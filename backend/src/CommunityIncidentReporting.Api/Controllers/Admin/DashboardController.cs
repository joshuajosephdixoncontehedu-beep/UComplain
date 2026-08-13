using CommunityIncidentReporting.Application.Features.Dashboard;
using CommunityIncidentReporting.Application.Features.Dashboard.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

public class DashboardController(IDashboardService dashboardService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        Ok(await dashboardService.GetAsync(from, to, cancellationToken));
}
