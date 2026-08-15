using CommunityIncidentReporting.Application.Common.Models;
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
public class ReportsController(IMobileReportService reportService, IMediaAttachmentService mediaAttachmentService)
    : MobileControllerBase
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
        [FromQuery] PagedRequest query, CancellationToken cancellationToken) =>
        Ok(await reportService.GetMyReportsAsync(CurrentReporterId, query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MobileReportDetailDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await reportService.GetByIdAsync(id, CurrentReporterId, cancellationToken));

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
}
