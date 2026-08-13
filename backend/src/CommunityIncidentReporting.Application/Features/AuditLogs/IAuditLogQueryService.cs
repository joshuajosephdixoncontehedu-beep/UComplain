using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.AuditLogs.Dtos;

namespace CommunityIncidentReporting.Application.Features.AuditLogs;

public interface IAuditLogQueryService
{
    Task<PagedResult<AuditLogListItemDto>> GetAllAsync(GetAuditLogsQuery query, CancellationToken cancellationToken);
}
