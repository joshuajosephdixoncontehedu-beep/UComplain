using CommunityIncidentReporting.Application.Common.Models;

namespace CommunityIncidentReporting.Application.Features.AuditLogs.Dtos;

public record GetAuditLogsQuery : PagedRequest
{
    public Guid? AdminUserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}
