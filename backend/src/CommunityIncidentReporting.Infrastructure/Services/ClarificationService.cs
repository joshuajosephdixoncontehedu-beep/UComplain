using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.Clarifications;
using CommunityIncidentReporting.Application.Features.Clarifications.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class ClarificationService(AppDbContext db, IAuditLogger auditLogger) : IClarificationService
{
    public async Task<IReadOnlyList<ClarificationRequestDto>> GetForReportAsync(
        Guid reportId, Guid reporterId, CancellationToken cancellationToken)
    {
        var reportOwned = await db.IncidentReports.AnyAsync(
            r => r.Id == reportId && r.ReporterId == reporterId, cancellationToken);
        if (!reportOwned)
        {
            throw new NotFoundException(nameof(IncidentReport), reportId);
        }

        var requests = await db.ClarificationRequests
            .Include(c => c.Responses)
            .Where(c => c.IncidentReportId == reportId)
            .OrderBy(c => c.RequestedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(ToDto).ToList();
    }

    public async Task<ClarificationResponseDto> ReplyAsync(
        Guid clarificationRequestId, Guid reporterId, ReplyToClarificationRequest request,
        string? requestIp, string? userAgent, CancellationToken cancellationToken)
    {
        var clarification = await db.ClarificationRequests
            .Include(c => c.IncidentReport)
            .FirstOrDefaultAsync(c => c.Id == clarificationRequestId, cancellationToken);

        if (clarification?.IncidentReport is null || clarification.IncidentReport.ReporterId != reporterId)
        {
            throw new NotFoundException(nameof(ClarificationRequest), clarificationRequestId);
        }

        if (clarification.IncidentReport.VerificationStatus != VerificationStatus.NeedsClarification)
        {
            throw new BusinessRuleException(
                "This clarification request is no longer open — the report has already moved on.");
        }

        if (request.AttachmentId is { } attachmentId)
        {
            var attachmentExists = await db.IncidentMediaAttachments.AnyAsync(
                a => a.Id == attachmentId && a.IncidentReportId == clarification.IncidentReportId && !a.IsDeleted,
                cancellationToken);
            if (!attachmentExists)
            {
                throw new NotFoundException(nameof(IncidentMediaAttachment), attachmentId);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var response = new ClarificationResponse
        {
            Id = Guid.NewGuid(),
            ClarificationRequestId = clarificationRequestId,
            Message = request.Message.Trim(),
            AttachmentId = request.AttachmentId,
            RespondedAt = now
        };
        db.ClarificationResponses.Add(response);

        // Only the first reply resolves it — later back-and-forth on the same request
        // doesn't need to re-resolve anything already resolved.
        clarification.ResolvedAt ??= now;

        await auditLogger.LogAsync(
            adminUserId: null, "ClarificationReplySubmitted", nameof(IncidentReport),
            clarification.IncidentReportId.ToString(), previousValue: null,
            newValue: new { ClarificationRequestId = clarificationRequestId, ResponseId = response.Id },
            requestIp, userAgent, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new ClarificationResponseDto(response.Id, response.Message, response.AttachmentId, response.RespondedAt);
    }

    private static ClarificationRequestDto ToDto(ClarificationRequest c) => new(
        c.Id, c.Message, c.RequestedAt, c.DueAt, c.ResolvedAt, c.AutoClosedAt,
        c.Responses.OrderBy(r => r.RespondedAt)
            .Select(r => new ClarificationResponseDto(r.Id, r.Message, r.AttachmentId, r.RespondedAt))
            .ToList());
}
