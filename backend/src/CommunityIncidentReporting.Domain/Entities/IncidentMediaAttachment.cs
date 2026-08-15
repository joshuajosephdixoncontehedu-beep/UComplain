using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// Metadata for one media file attached to an IncidentReport. The file itself lives in
/// Supabase Storage (a private bucket), never in Postgres — StoragePath is the
/// server-generated object key, and access is always mediated through a short-lived
/// signed URL issued to an authorized caller, never a permanent public URL.
/// </summary>
public class IncidentMediaAttachment
{
    public Guid Id { get; set; }

    public Guid IncidentReportId { get; set; }
    public IncidentReport? IncidentReport { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Optional cached reference (e.g. the storage object id) — never a permanent public
    /// URL. Actual access always goes through a freshly-issued signed URL.
    /// </summary>
    public string? PublicOrSignedUrlReference { get; set; }

    public MediaType MediaType { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public Guid? UploadedByReporterId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
