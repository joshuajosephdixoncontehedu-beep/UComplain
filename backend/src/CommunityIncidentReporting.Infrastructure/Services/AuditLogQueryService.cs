using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.AuditLogs;
using CommunityIncidentReporting.Application.Features.AuditLogs.Dtos;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class AuditLogQueryService(AppDbContext db) : IAuditLogQueryService
{
    public async Task<PagedResult<AuditLogListItemDto>> GetAllAsync(
        GetAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var logs = db.AuditLogs.AsQueryable();

        if (query.AdminUserId is { } adminUserId)
        {
            logs = logs.Where(l => l.AdminUserId == adminUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            logs = logs.Where(l => l.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            logs = logs.Where(l => l.EntityType == query.EntityType);
        }

        if (query.From is { } from)
        {
            logs = logs.Where(l => l.CreatedAt >= from);
        }

        if (query.To is { } to)
        {
            logs = logs.Where(l => l.CreatedAt <= to);
        }

        var total = await logs.CountAsync(cancellationToken);

        var items = await logs
            .OrderByDescending(l => l.CreatedAt).ThenBy(l => l.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new AuditLogListItemDto(
                l.Id, l.AdminUserId, l.AdminUser != null ? l.AdminUser.FullName : null, l.Action, l.EntityType,
                l.EntityId, l.PreviousValueJson, l.NewValueJson, l.IpAddress, l.UserAgent, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogListItemDto> { Items = items, Total = total, Page = query.Page, PageSize = query.PageSize };
    }
}
