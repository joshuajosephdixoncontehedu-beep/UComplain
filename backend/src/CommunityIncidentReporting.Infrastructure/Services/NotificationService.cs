using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class NotificationService(AppDbContext db) : INotificationService
{
    public Task NotifyAsync(
        Guid reporterId, NotificationType type, string title, string body, Guid? reportId,
        CancellationToken cancellationToken)
    {
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            Type = type,
            Title = title,
            Body = body,
            ReportId = reportId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        // Deliberately not calling SaveChangesAsync here — same reasoning as
        // AuditLogger.LogAsync: the caller's own SaveChangesAsync persists this in the
        // same transaction as whatever change triggered it.
        return Task.CompletedTask;
    }
}
