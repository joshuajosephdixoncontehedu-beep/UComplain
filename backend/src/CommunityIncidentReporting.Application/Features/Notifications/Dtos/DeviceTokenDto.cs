using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Notifications.Dtos;

public record DeviceTokenDto(Guid Id, DevicePlatform Platform, DateTimeOffset LastSeenAt);
