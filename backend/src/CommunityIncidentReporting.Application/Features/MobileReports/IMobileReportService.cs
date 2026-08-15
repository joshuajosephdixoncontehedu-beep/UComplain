using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

namespace CommunityIncidentReporting.Application.Features.MobileReports;

/// <summary>
/// Mobile-app incident report submission and self-service viewing — unlike
/// IIncidentReportService (the admin operational queue, Verified-only), this exposes a
/// reporter's own reports regardless of verification status, since the reporter needs to
/// track a report from submission through verification.
/// </summary>
public interface IMobileReportService
{
    /// <summary>ReporterId always comes from the caller's JWT claim, never the request body.</summary>
    Task<MobileReportDetailDto> CreateAsync(
        CreateMobileReportRequest request, Guid reporterId, string? requestIp, string? userAgent,
        CancellationToken cancellationToken);

    Task<PagedResult<MobileReportListItemDto>> GetMyReportsAsync(
        Guid reporterId, GetMyReportsQuery query, CancellationToken cancellationToken);

    Task<ReportCountsDto> GetMyReportCountsAsync(Guid reporterId, CancellationToken cancellationToken);

    /// <summary>Throws NotFoundException if the report doesn't exist or isn't owned by reporterId — the two cases are indistinguishable to the caller.</summary>
    Task<MobileReportDetailDto> GetByIdAsync(Guid reportId, Guid reporterId, CancellationToken cancellationToken);

    /// <summary>Chronological (oldest first) — the reverse order of MobileReportDetailDto.StatusHistory, which is newest-first.</summary>
    Task<IReadOnlyList<MobileReportStatusHistoryDto>> GetTimelineAsync(
        Guid reportId, Guid reporterId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportInformationDto>> GetInformationAsync(
        Guid reportId, Guid reporterId, CancellationToken cancellationToken);

    /// <summary>
    /// Throws BusinessRuleException if the report is not in the Active bucket (see
    /// ReportStatusProjection), or NotFoundException if AttachmentId is supplied but
    /// isn't an existing, non-deleted attachment on this same report.
    /// </summary>
    Task<ReportInformationDto> AddInformationAsync(
        Guid reportId, Guid reporterId, AddReportInformationRequest request, string? requestIp, string? userAgent,
        CancellationToken cancellationToken);

    /// <summary>
    /// Throws BusinessRuleException unless the report's CaseStatus is
    /// VerificationPending, UnderReview, or Assigned — once an officer has started
    /// active work (InProgress or further) a reporter can no longer unilaterally
    /// withdraw it.
    /// </summary>
    Task<MobileReportDetailDto> WithdrawAsync(
        Guid reportId, Guid reporterId, WithdrawReportRequest request, string? requestIp, string? userAgent,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MobileCategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken);

    Task<DraftDto> CreateDraftAsync(Guid reporterId, CancellationToken cancellationToken);

    /// <summary>
    /// Full-replace of every mutable field — see UpdateDraftRequest. Throws
    /// NotFoundException if the draft doesn't exist/isn't owned, BusinessRuleException if
    /// it's already been submitted, and NotFoundException if a supplied CategoryId isn't
    /// a real active category.
    /// </summary>
    Task<DraftDto> UpdateDraftAsync(
        Guid draftId, Guid reporterId, UpdateDraftRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Idempotent: submitting an already-submitted draft again just returns the report
    /// created the first time, rather than erroring or creating a duplicate. Throws
    /// BusinessRuleException if the truth declaration isn't accepted or the draft is
    /// missing required fields (category/description/incidentOccurredAt/location).
    /// </summary>
    Task<MobileReportDetailDto> SubmitDraftAsync(
        Guid draftId, Guid reporterId, SubmitDraftRequest request, string? requestIp, string? userAgent,
        CancellationToken cancellationToken);
}
