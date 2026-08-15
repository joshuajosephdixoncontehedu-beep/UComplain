using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Notifications.Dtos;

/// <summary>
/// Upsert semantics by Token: registering a token already on file reassigns it to the
/// calling reporter and refreshes LastSeenAt/Platform, rather than erroring — see
/// DeviceToken's doc comment.
/// </summary>
public record RegisterDeviceRequest(DevicePlatform Platform, string Token);
