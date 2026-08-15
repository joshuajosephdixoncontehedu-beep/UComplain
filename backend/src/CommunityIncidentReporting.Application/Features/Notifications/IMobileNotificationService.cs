using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Notifications.Dtos;

namespace CommunityIncidentReporting.Application.Features.Notifications;

/// <summary>
/// Reporter-facing notification inbox and device-token registration. Writing a
/// Notification row happens elsewhere (see Application.Common.Interfaces.INotificationService,
/// called from the transition points that raise one) — this is only the read/manage side.
/// </summary>
public interface IMobileNotificationService
{
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(
        Guid reporterId, PagedRequest query, CancellationToken cancellationToken);

    /// <summary>Idempotent — marking an already-read notification read again is a no-op. Throws NotFoundException if not owned.</summary>
    Task<NotificationDto> MarkReadAsync(Guid notificationId, Guid reporterId, CancellationToken cancellationToken);

    Task<MarkAllReadResponse> MarkAllReadAsync(Guid reporterId, CancellationToken cancellationToken);

    /// <summary>Upsert by Token — see RegisterDeviceRequest's doc comment.</summary>
    Task<DeviceTokenDto> RegisterDeviceAsync(
        Guid reporterId, RegisterDeviceRequest request, CancellationToken cancellationToken);

    /// <summary>Idempotent — revoking an already-revoked device is a no-op. Throws NotFoundException if not owned.</summary>
    Task RevokeDeviceAsync(Guid deviceId, Guid reporterId, CancellationToken cancellationToken);
}
