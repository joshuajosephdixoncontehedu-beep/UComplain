using System.Text.Json;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class AuditLogger(AppDbContext db) : IAuditLogger
{
    public async Task LogAsync(
        Guid? adminUserId,
        string action,
        string entityType,
        string entityId,
        object? previousValue,
        object? newValue,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PreviousValueJson = previousValue is null ? null : JsonSerializer.Serialize(previousValue),
            NewValueJson = newValue is null ? null : JsonSerializer.Serialize(newValue),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTimeOffset.UtcNow
        });

        // Deliberately not calling SaveChangesAsync here — the caller's own
        // SaveChangesAsync (after making its own entity changes) persists this in the
        // same transaction, so an audit row is never written for a change that itself
        // failed to save.
        await Task.CompletedTask;
    }
}
