using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Features.Settings;
using CommunityIncidentReporting.Application.Features.Settings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

[Authorize(Policy = Policies.SuperAdminOnly)]
public class SettingsController(ISettingsService settingsService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SettingsDto>> Get(CancellationToken cancellationToken) =>
        Ok(await settingsService.GetAsync(cancellationToken));

    [HttpPatch]
    public async Task<ActionResult<SettingsDto>> Update(
        [FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken) =>
        Ok(await settingsService.UpdateAsync(request, BuildRequestContext(), cancellationToken));
}
