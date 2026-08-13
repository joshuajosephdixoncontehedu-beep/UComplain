using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Features.Administrators;
using CommunityIncidentReporting.Application.Features.Administrators.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

[Authorize(Policy = Policies.SuperAdminOnly)]
public class AdministratorsController(IAdministratorService administratorService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdministratorDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await administratorService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdministratorDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await administratorService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AdministratorDto>> Create(
        [FromBody] CreateAdministratorRequest request, CancellationToken cancellationToken)
    {
        var admin = await administratorService.CreateAsync(request, BuildRequestContext(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = admin.Id }, admin);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<AdministratorDto>> Update(
        Guid id, [FromBody] UpdateAdministratorRequest request, CancellationToken cancellationToken) =>
        Ok(await administratorService.UpdateAsync(id, request, BuildRequestContext(), cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<AdministratorDto>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await administratorService.DeactivateAsync(id, BuildRequestContext(), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<AdministratorDto>> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await administratorService.ReactivateAsync(id, BuildRequestContext(), cancellationToken));
}
