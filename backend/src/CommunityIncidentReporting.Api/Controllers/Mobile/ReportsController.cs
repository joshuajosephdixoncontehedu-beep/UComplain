using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Clarifications;
using CommunityIncidentReporting.Application.Features.Clarifications.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>
/// Mobile reporter incident submission and self-service viewing (api/mobile/reports —
/// via MobileControllerBase's [controller] route token). Every action is scoped to the
/// authenticated reporter (CurrentReporterId, from MobileControllerBase); a reporter can
/// never read or act on another reporter's report or attachment.
/// </summary>
public class ReportsController(
    IMobileReportService reportService, IMediaAttachmentService mediaAttachmentService,
    IClarificationService clarificationService) : MobileControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MobileReportDetailDto>> Create(
        [FromBody] CreateMobileReportRequest request, CancellationToken cancellationToken)
    {
        var response = await reportService.CreateAsync(
            request, CurrentReporterId, RemoteIpAddress, UserAgentHeader, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MobileReportListItemDto>>> GetMyReports(
        [FromQuery] GetMyReportsQuery query, CancellationToken cancellationToken) =>
        Ok(await reportService.GetMyReportsAsync(CurrentReporterId, query, cancellationToken));

    // "counts" as a literal path segment never collides with GetById's {id:guid} route
    // below — same reasoning as "drafts" further down.
    [HttpGet("counts")]
    public async Task<ActionResult<ReportCountsDto>> GetCounts(CancellationToken cancellationToken) =>
        Ok(await reportService.GetMyReportCountsAsync(CurrentReporterId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MobileReportDetailDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await reportService.GetByIdAsync(id, CurrentReporterId, cancellationToken));

    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<IReadOnlyList<MobileReportStatusHistoryDto>>> GetTimeline(
        Guid id, CancellationToken cancellationToken) =>
        Ok(await reportService.GetTimelineAsync(id, CurrentReporterId, cancellationToken));

    [HttpGet("{id:guid}/information")]
    public async Task<ActionResult<IReadOnlyList<ReportInformationDto>>> GetInformation(
        Guid id, CancellationToken cancellationToken) =>
        Ok(await reportService.GetInformationAsync(id, CurrentReporterId, cancellationToken));

    [HttpPost("{id:guid}/information")]
    public async Task<ActionResult<ReportInformationDto>> AddInformation(
        Guid id, [FromBody] AddReportInformationRequest request, CancellationToken cancellationToken) =>
        Ok(await reportService.AddInformationAsync(
            id, CurrentReporterId, request, RemoteIpAddress, UserAgentHeader, cancellationToken));

    [HttpGet("{id:guid}/clarifications")]
    public async Task<ActionResult<IReadOnlyList<ClarificationRequestDto>>> GetClarifications(
        Guid id, CancellationToken cancellationToken) =>
        Ok(await clarificationService.GetForReportAsync(id, CurrentReporterId, cancellationToken));

    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<MobileReportDetailDto>> Withdraw(
        Guid id, [FromBody] WithdrawReportRequest request, CancellationToken cancellationToken) =>
        Ok(await reportService.WithdrawAsync(
            id, CurrentReporterId, request, RemoteIpAddress, UserAgentHeader, cancellationToken));

    [HttpPost("{id:guid}/attachments")]
    [RequestFormLimits(MultipartBodyLengthLimit = 150_000_000)]
    [RequestSizeLimit(150_000_000)]
    public async Task<ActionResult<IReadOnlyList<MediaAttachmentDto>>> UploadAttachments(
        Guid id, [FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        var uploadFiles = files.Select(f => new MediaUploadFile(f.FileName, f.ContentType, f.Length, f.OpenReadStream())).ToList();
        try
        {
            var attachments = await mediaAttachmentService.UploadAsync(id, CurrentReporterId, uploadFiles, cancellationToken);
            return Ok(attachments);
        }
        finally
        {
            foreach (var file in uploadFiles)
            {
                await file.Content.DisposeAsync();
            }
        }
    }

    [HttpDelete("{reportId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid reportId, Guid attachmentId, CancellationToken cancellationToken)
    {
        await mediaAttachmentService.DeleteAsync(reportId, attachmentId, CurrentReporterId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{reportId:guid}/attachments/{attachmentId:guid}/access-url")]
    public async Task<ActionResult<SignedUrlResponse>> GetAttachmentAccessUrl(
        Guid reportId, Guid attachmentId, CancellationToken cancellationToken) =>
        Ok(await mediaAttachmentService.GetReporterAccessUrlAsync(reportId, attachmentId, CurrentReporterId, cancellationToken));

    // --- Draft-based report wizard (Phase 3) ---
    // "drafts" as a literal path segment never collides with GetById's {id:guid} route
    // above — "drafts" isn't a valid GUID, so the :guid constraint rules it out.

    [HttpPost("drafts")]
    public async Task<ActionResult<DraftDto>> CreateDraft(CancellationToken cancellationToken) =>
        Ok(await reportService.CreateDraftAsync(CurrentReporterId, cancellationToken));

    [HttpPatch("drafts/{id:guid}")]
    public async Task<ActionResult<DraftDto>> UpdateDraft(
        Guid id, [FromBody] UpdateDraftRequest request, CancellationToken cancellationToken) =>
        Ok(await reportService.UpdateDraftAsync(id, CurrentReporterId, request, cancellationToken));

    [HttpPost("drafts/{id:guid}/attachments")]
    [RequestFormLimits(MultipartBodyLengthLimit = 150_000_000)]
    [RequestSizeLimit(150_000_000)]
    public async Task<ActionResult<IReadOnlyList<MediaAttachmentDto>>> UploadDraftAttachments(
        Guid id, [FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        var uploadFiles = files.Select(f => new MediaUploadFile(f.FileName, f.ContentType, f.Length, f.OpenReadStream())).ToList();
        try
        {
            var attachments = await mediaAttachmentService.UploadToDraftAsync(id, CurrentReporterId, uploadFiles, cancellationToken);
            return Ok(attachments);
        }
        finally
        {
            foreach (var file in uploadFiles)
            {
                await file.Content.DisposeAsync();
            }
        }
    }

    [HttpDelete("drafts/{draftId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteDraftAttachment(
        Guid draftId, Guid attachmentId, CancellationToken cancellationToken)
    {
        await mediaAttachmentService.DeleteDraftAttachmentAsync(draftId, attachmentId, CurrentReporterId, cancellationToken);
        return NoContent();
    }

    [HttpPost("drafts/{id:guid}/submit")]
    public async Task<ActionResult<MobileReportDetailDto>> SubmitDraft(
        Guid id, [FromBody] SubmitDraftRequest request, CancellationToken cancellationToken) =>
        Ok(await reportService.SubmitDraftAsync(
            id, CurrentReporterId, request, RemoteIpAddress, UserAgentHeader, cancellationToken));
}
