using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Notifications.Dtos;

public record NotificationDto(
    Guid Id, NotificationType Type, string Title, string Body, Guid? ReportId,
    DateTimeOffset? ReadAt, DateTimeOffset CreatedAt);
