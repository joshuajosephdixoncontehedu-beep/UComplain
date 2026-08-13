namespace CommunityIncidentReporting.Application.Features.AuditLogs.Dtos;

public record AuditLogListItemDto(
    Guid Id,
    Guid? AdminUserId,
    string? AdminUserName,
    string Action,
    string EntityType,
    string EntityId,
    string? PreviousValueJson,
    string? NewValueJson,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAt);
