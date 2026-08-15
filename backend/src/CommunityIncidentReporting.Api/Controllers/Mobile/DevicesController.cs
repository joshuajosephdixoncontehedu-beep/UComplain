using CommunityIncidentReporting.Application.Features.Notifications;
using CommunityIncidentReporting.Application.Features.Notifications.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>Push-notification device-token registration (api/mobile/devices). Persistence only — no push send in this phase.</summary>
public class DevicesController(IMobileNotificationService notificationService) : MobileControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DeviceTokenDto>> Register(
        [FromBody] RegisterDeviceRequest request, CancellationToken cancellationToken) =>
        Ok(await notificationService.RegisterDeviceAsync(CurrentReporterId, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.RevokeDeviceAsync(id, CurrentReporterId, cancellationToken);
        return NoContent();
    }
}
