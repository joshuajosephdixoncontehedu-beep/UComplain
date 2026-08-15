using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>
/// Draft-scoped counterpart to IncidentMediaAttachment. No IsDeleted/DeletedAt (unlike
/// the submitted-report version) — a draft is ephemeral, pre-submission scratch data, so
/// deleting an attachment here just removes the row outright rather than needing an
/// audit-safe soft-delete. At submit, each row is re-parented into a real
/// IncidentMediaAttachment (same StoragePath, no re-upload) and this row is removed.
/// </summary>
public class ReportDraftAttachment
{
    public Guid Id { get; set; }

    public Guid ReportDraftId { get; set; }
    public ReportDraft? ReportDraft { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
