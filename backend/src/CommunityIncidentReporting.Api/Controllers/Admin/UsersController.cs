using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Reporters;
using CommunityIncidentReporting.Application.Features.Reporters.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

public class UsersController(IReporterService reporterService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReporterListItemDto>>> GetAll(
        [FromQuery] GetReportersQuery query, CancellationToken cancellationToken) =>
        Ok(await reporterService.GetAllAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReporterDetailDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await reporterService.GetByIdAsync(id, cancellationToken));

    [HttpPost("{id:guid}/restrict")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<ReporterDetailDto>> Restrict(Guid id, CancellationToken cancellationToken) =>
        Ok(await reporterService.RestrictAsync(id, BuildRequestContext(), cancellationToken));

    [HttpPost("{id:guid}/unrestrict")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<ReporterDetailDto>> Unrestrict(Guid id, CancellationToken cancellationToken) =>
        Ok(await reporterService.UnrestrictAsync(id, BuildRequestContext(), cancellationToken));
}
