using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Notifications;
using CommunityIncidentReporting.Application.Features.Notifications.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>Reporter-facing notification inbox (api/mobile/notifications). Device-token registration lives on DevicesController.</summary>
public class NotificationsController(IMobileNotificationService notificationService) : MobileControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetMyNotifications(
        [FromQuery] PagedRequest query, CancellationToken cancellationToken) =>
        Ok(await notificationService.GetMyNotificationsAsync(CurrentReporterId, query, cancellationToken));

    // "read-all" as a literal path segment never collides with MarkRead's {id:guid} route
    // below — "read-all" isn't a valid GUID, so the :guid constraint rules it out.
    [HttpPost("read-all")]
    public async Task<ActionResult<MarkAllReadResponse>> MarkAllRead(CancellationToken cancellationToken) =>
        Ok(await notificationService.MarkAllReadAsync(CurrentReporterId, cancellationToken));

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<NotificationDto>> MarkRead(Guid id, CancellationToken cancellationToken) =>
        Ok(await notificationService.MarkReadAsync(id, CurrentReporterId, cancellationToken));
}
