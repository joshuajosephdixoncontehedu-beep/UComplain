using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileReports;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Application.Features.Reports;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using CommunityIncidentReporting.Application.Features.Verification;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

public class ReportsController(
    IIncidentReportService reportService, IVerificationService verificationService,
    IMediaAttachmentService mediaAttachmentService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<IncidentReportListItemDto>>> GetAll(
        [FromQuery] GetReportsQuery query, CancellationToken cancellationToken) =>
        Ok(await reportService.GetAllAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentReportDetailDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await reportService.GetByIdAsync(id, cancellationToken));

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Policies.ReviewerOrAbove)]
    public async Task<ActionResult<IncidentReportDetailDto>> Update(
        Guid id, [FromBody] UpdateReportRequest request, CancellationToken cancellationToken) =>
        Ok(await reportService.UpdateAsync(id, request, BuildRequestContext(), cancellationToken));

    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<IncidentReportDetailDto>> Assign(
        Guid id, [FromBody] AssignReportRequest request, CancellationToken cancellationToken) =>
        Ok(await reportService.AssignAsync(id, request, BuildRequestContext(), cancellationToken));

    [HttpPost("{id:guid}/notes")]
    [Authorize(Policy = Policies.ReviewerOrAbove)]
    public async Task<ActionResult<IncidentReportDetailDto>> AddNote(
        Guid id, [FromBody] AddNoteRequest request, CancellationToken cancellationToken) =>
        Ok(await reportService.AddNoteAsync(id, request, BuildRequestContext(), cancellationToken));

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = Policies.ReviewerOrAbove)]
    public async Task<ActionResult<IncidentReportDetailDto>> ChangeStatus(
        Guid id, [FromBody] ChangeStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await reportService.ChangeStatusAsync(id, request, BuildRequestContext(), cancellationToken));

    [HttpPost("{id:guid}/verification-decision")]
    [Authorize(Policy = Policies.ReviewerOrAbove)]
    public async Task<ActionResult<IncidentReportDetailDto>> VerificationDecision(
        Guid id, [FromBody] VerificationDecisionRequest request, CancellationToken cancellationToken) =>
        Ok(await verificationService.DecideAsync(id, request, BuildRequestContext(), cancellationToken));

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/access-url")]
    [Authorize(Policy = Policies.ReviewerOrAbove)]
    public async Task<ActionResult<SignedUrlResponse>> GetAttachmentAccessUrl(
        Guid id, Guid attachmentId, CancellationToken cancellationToken) =>
        Ok(await mediaAttachmentService.GetAdminAccessUrlAsync(id, attachmentId, cancellationToken));
}
