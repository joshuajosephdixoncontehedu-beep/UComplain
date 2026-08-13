namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record AuditLogEntryDto(
    Guid Id,
    Guid? AdminUserId,
    string? AdminUserName,
    string Action,
    string? PreviousValueJson,
    string? NewValueJson,
    DateTimeOffset CreatedAt);
