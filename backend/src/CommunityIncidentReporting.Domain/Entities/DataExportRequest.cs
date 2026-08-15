using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// A reporter's request for a downloadable copy of their own data. StoragePath is set
/// once Completed — deliberately never a stored signed/download URL (same convention as
/// IncidentMediaAttachment.StoragePath): a fresh signed URL is issued on every read via
/// ISupabaseStorageService, never persisted.
/// </summary>
public class DataExportRequest
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }
    public Reporter? Reporter { get; set; }

    public DataExportStatus Status { get; set; } = DataExportStatus.Pending;
    public string? StoragePath { get; set; }
    public string? FailureReason { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
