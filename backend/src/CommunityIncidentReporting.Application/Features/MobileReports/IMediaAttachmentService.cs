using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

namespace CommunityIncidentReporting.Application.Features.MobileReports;

/// <summary>
/// Media attachment upload/delete/access — shared between the mobile reporter endpoints
/// (ownership-checked) and the admin attachment-access endpoint (Phase 6, any
/// authenticated admin, no ownership check). Storage writes go through
/// ISupabaseStorageService; a database row is only written after the storage upload
/// succeeds, and a batch upload cleans up any files it already stored if a later file in
/// the same request fails validation or upload.
/// </summary>
public interface IMediaAttachmentService
{
    /// <summary>
    /// Throws NotFoundException if the report doesn't exist or isn't owned by
    /// reporterId, and BusinessRuleException if the report is no longer in a state that
    /// allows attachment changes, or if per-type count/size limits would be exceeded.
    /// </summary>
    Task<IReadOnlyList<MediaAttachmentDto>> UploadAsync(
        Guid reportId, Guid reporterId, IReadOnlyList<MediaUploadFile> files, CancellationToken cancellationToken);

    Task DeleteAsync(Guid reportId, Guid attachmentId, Guid reporterId, CancellationToken cancellationToken);

    Task<SignedUrlResponse> GetReporterAccessUrlAsync(
        Guid reportId, Guid attachmentId, Guid reporterId, CancellationToken cancellationToken);

    /// <summary>No ownership check — any authenticated admin may request a signed URL for any report's attachment.</summary>
    Task<SignedUrlResponse> GetAdminAccessUrlAsync(Guid reportId, Guid attachmentId, CancellationToken cancellationToken);

    /// <summary>Same validation as UploadAsync, against a not-yet-submitted ReportDraft instead of a submitted report.</summary>
    Task<IReadOnlyList<MediaAttachmentDto>> UploadToDraftAsync(
        Guid draftId, Guid reporterId, IReadOnlyList<MediaUploadFile> files, CancellationToken cancellationToken);

    Task DeleteDraftAttachmentAsync(Guid draftId, Guid attachmentId, Guid reporterId, CancellationToken cancellationToken);
}
