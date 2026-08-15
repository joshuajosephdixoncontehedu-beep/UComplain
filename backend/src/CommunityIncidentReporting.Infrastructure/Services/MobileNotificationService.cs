using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Notifications;
using CommunityIncidentReporting.Application.Features.Notifications.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class MobileNotificationService(AppDbContext db) : IMobileNotificationService
{
    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(
        Guid reporterId, PagedRequest query, CancellationToken cancellationToken)
    {
        var notifications = db.Notifications.Where(n => n.ReporterId == reporterId);

        var total = await notifications.CountAsync(cancellationToken);

        var items = await notifications
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.ReportId, n.ReadAt, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto> { Items = items, Total = total, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<NotificationDto> MarkReadAsync(Guid notificationId, Guid reporterId, CancellationToken cancellationToken)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(
            n => n.Id == notificationId && n.ReporterId == reporterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), notificationId);

        notification.ReadAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new NotificationDto(
            notification.Id, notification.Type, notification.Title, notification.Body, notification.ReportId,
            notification.ReadAt, notification.CreatedAt);
    }

    public async Task<MarkAllReadResponse> MarkAllReadAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var unread = await db.Notifications
            .Where(n => n.ReporterId == reporterId && n.ReadAt == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.ReadAt = now;
        }

        if (unread.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new MarkAllReadResponse(unread.Count);
    }

    public async Task<DeviceTokenDto> RegisterDeviceAsync(
        Guid reporterId, RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var existing = await db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == request.Token, cancellationToken);

        if (existing is not null)
        {
            existing.ReporterId = reporterId;
            existing.Platform = request.Platform;
            existing.LastSeenAt = now;
            existing.RevokedAt = null;
            await db.SaveChangesAsync(cancellationToken);
            return new DeviceTokenDto(existing.Id, existing.Platform, existing.LastSeenAt);
        }

        var device = new DeviceToken
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            Platform = request.Platform,
            Token = request.Token,
            LastSeenAt = now,
            CreatedAt = now
        };
        db.DeviceTokens.Add(device);
        await db.SaveChangesAsync(cancellationToken);

        return new DeviceTokenDto(device.Id, device.Platform, device.LastSeenAt);
    }

    public async Task RevokeDeviceAsync(Guid deviceId, Guid reporterId, CancellationToken cancellationToken)
    {
        var device = await db.DeviceTokens.FirstOrDefaultAsync(
            d => d.Id == deviceId && d.ReporterId == reporterId, cancellationToken)
            ?? throw new NotFoundException(nameof(DeviceToken), deviceId);

        device.RevokedAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
