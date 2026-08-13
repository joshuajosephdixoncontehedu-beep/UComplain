namespace CommunityIncidentReporting.Application.Common.Interfaces;

/// <summary>
/// Writes an AuditLog row. Every state-changing admin action calls this — see
/// docs/api-contract.md's "Every state-changing endpoint" section. previousValue/
/// newValue are serialized to JSON as-is; pass an anonymous object or DTO capturing
/// only the fields that changed, not the full entity.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        Guid? adminUserId,
        string action,
        string entityType,
        string entityId,
        object? previousValue,
        object? newValue,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
