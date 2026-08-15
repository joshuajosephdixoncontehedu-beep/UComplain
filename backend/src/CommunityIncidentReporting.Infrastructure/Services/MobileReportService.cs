using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileReports;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class MobileReportService(AppDbContext db, IAuditLogger auditLogger) : IMobileReportService
{
    public async Task<MobileReportDetailDto> CreateAsync(
        CreateMobileReportRequest request, Guid reporterId, string? requestIp, string? userAgent,
        CancellationToken cancellationToken)
    {
        var category = await db.IncidentCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken)
            ?? throw new NotFoundException(nameof(IncidentCategory), request.CategoryId);

        var now = DateTimeOffset.UtcNow;

        var report = new IncidentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            CategoryId = category.Id,
            SourceChannel = SourceChannel.MobileApp,
            Description = request.Description.Trim(),
            IncidentOccurredAt = request.IncidentOccurredAt,
            LocationDescription = request.LocationDescription.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            // Server-controlled — see CreateMobileReportRequest's doc comment.
            Priority = category.DefaultPriority,
            VerificationStatus = VerificationStatus.Pending,
            CaseStatus = CaseStatus.VerificationPending,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.IncidentReports.Add(report);

        await auditLogger.LogAsync(
            adminUserId: null, "ReportSubmittedViaMobileApp", nameof(IncidentReport), report.Id.ToString(),
            previousValue: null,
            newValue: new { report.CategoryId, ReporterId = reporterId, request.InitialPrioritySignal },
            requestIp, userAgent, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await BuildDetailDtoAsync(report, category.Name, cancellationToken);
    }

    public async Task<PagedResult<MobileReportListItemDto>> GetMyReportsAsync(
        Guid reporterId, PagedRequest query, CancellationToken cancellationToken)
    {
        var reports = db.IncidentReports.Where(r => r.ReporterId == reporterId);

        var total = await reports.CountAsync(cancellationToken);

        var items = await reports
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new MobileReportListItemDto(
                r.Id, r.CaseReference, r.Category!.Name, r.CreatedAt, r.Priority, r.VerificationStatus, r.CaseStatus,
                r.MediaAttachments.Count(a => !a.IsDeleted)))
            .ToListAsync(cancellationToken);

        return new PagedResult<MobileReportListItemDto>
        {
            Items = items, Total = total, Page = query.Page, PageSize = query.PageSize
        };
    }

    public async Task<MobileReportDetailDto> GetByIdAsync(Guid reportId, Guid reporterId, CancellationToken cancellationToken)
    {
        var report = await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);
        var categoryName = await db.IncidentCategories
            .Where(c => c.Id == report.CategoryId).Select(c => c.Name).FirstAsync(cancellationToken);
        return await BuildDetailDtoAsync(report, categoryName, cancellationToken);
    }

    internal async Task<IncidentReport> FindOwnedReportOrThrowAsync(
        Guid reportId, Guid reporterId, CancellationToken cancellationToken)
    {
        var report = await db.IncidentReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        // Deliberately the same exception/message whether the report doesn't exist at
        // all or simply isn't this reporter's — never confirm another reporter's report
        // id is valid.
        if (report is null || report.ReporterId != reporterId)
        {
            throw new NotFoundException(nameof(IncidentReport), reportId);
        }

        return report;
    }

    private async Task<MobileReportDetailDto> BuildDetailDtoAsync(
        IncidentReport report, string categoryName, CancellationToken cancellationToken)
    {
        var statusHistory = await db.StatusHistories
            .Where(s => s.IncidentReportId == report.Id)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new MobileReportStatusHistoryDto(s.PreviousStatus, s.NewStatus, s.CreatedAt))
            .ToListAsync(cancellationToken);

        var attachments = await db.IncidentMediaAttachments
            .Where(a => a.IncidentReportId == report.Id && !a.IsDeleted)
            .OrderBy(a => a.SortOrder)
            .Select(a => new MediaAttachmentDto(a.Id, a.FileName, a.MediaType, a.MimeType, a.FileSizeBytes, a.SortOrder, a.UploadedAt))
            .ToListAsync(cancellationToken);

        return new MobileReportDetailDto(
            report.Id, report.CaseReference, report.CategoryId, categoryName, report.SourceChannel,
            report.Description, report.IncidentOccurredAt, report.LocationDescription, report.Latitude,
            report.Longitude, report.VerificationStatus, report.CaseStatus, report.Priority,
            report.ResolutionSummary, report.CreatedAt, report.UpdatedAt, report.ClosedAt,
            statusHistory, attachments);
    }
}
