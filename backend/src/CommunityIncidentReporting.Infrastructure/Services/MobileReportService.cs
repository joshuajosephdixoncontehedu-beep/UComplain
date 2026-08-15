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

public class MobileReportService(AppDbContext db, IAuditLogger auditLogger, IReportVisibilityService visibilityService)
    : IMobileReportService
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
            Landmark = request.Landmark?.Trim(),
            // Server-controlled — see CreateMobileReportRequest's doc comment.
            Priority = category.DefaultPriority,
            VerificationStatus = VerificationStatus.Pending,
            CaseStatus = CaseStatus.VerificationPending,
            SubmittedAt = now,
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
        Guid reporterId, GetMyReportsQuery query, CancellationToken cancellationToken)
    {
        // ReportStatusProjection is a pure C# function over two already-loaded enum
        // values, not a SQL-translatable expression, so bucket filtering happens after
        // materializing — acceptable here because this is always scoped to one
        // reporter's own reports, not the full table.
        var candidates = await db.IncidentReports
            .Where(r => r.ReporterId == reporterId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id, r.CaseReference, CategoryName = r.Category!.Name, r.CreatedAt, r.Priority,
                r.VerificationStatus, r.CaseStatus, AttachmentCount = r.MediaAttachments.Count(a => !a.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        var projected = candidates
            .Select(r => (Row: r, Projection: ReportStatusProjection.Project(r.VerificationStatus, r.CaseStatus)))
            .Where(x => query.Status is null || x.Projection.Bucket == query.Status)
            .ToList();

        var items = projected
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new MobileReportListItemDto(
                x.Row.Id, x.Row.CaseReference, x.Row.CategoryName, x.Row.CreatedAt, x.Row.Priority,
                x.Row.VerificationStatus, x.Row.CaseStatus, x.Row.AttachmentCount,
                x.Projection.BadgeLabel, x.Projection.TrackerStage, x.Projection.ProgressPercent, x.Projection.Bucket))
            .ToList();

        return new PagedResult<MobileReportListItemDto>
        {
            Items = items, Total = projected.Count, Page = query.Page, PageSize = query.PageSize
        };
    }

    public async Task<ReportCountsDto> GetMyReportCountsAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var statuses = await db.IncidentReports
            .Where(r => r.ReporterId == reporterId)
            .Select(r => new { r.VerificationStatus, r.CaseStatus })
            .ToListAsync(cancellationToken);

        var buckets = statuses
            .Select(s => ReportStatusProjection.Project(s.VerificationStatus, s.CaseStatus).Bucket)
            .ToList();

        return new ReportCountsDto(
            buckets.Count(b => b == ReportListBucket.Active),
            buckets.Count(b => b == ReportListBucket.Resolved),
            buckets.Count(b => b == ReportListBucket.Rejected),
            statuses.Count);
    }

    public async Task<MobileReportDetailDto> GetByIdAsync(Guid reportId, Guid reporterId, CancellationToken cancellationToken)
    {
        var report = await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);
        var categoryName = await db.IncidentCategories
            .Where(c => c.Id == report.CategoryId).Select(c => c.Name).FirstAsync(cancellationToken);
        return await BuildDetailDtoAsync(report, categoryName, cancellationToken);
    }

    public async Task<IReadOnlyList<MobileReportStatusHistoryDto>> GetTimelineAsync(
        Guid reportId, Guid reporterId, CancellationToken cancellationToken)
    {
        await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);

        return await db.StatusHistories
            .Where(s => s.IncidentReportId == reportId)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new MobileReportStatusHistoryDto(s.PreviousStatus, s.NewStatus, s.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReportInformationDto>> GetInformationAsync(
        Guid reportId, Guid reporterId, CancellationToken cancellationToken)
    {
        await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);

        return await db.ReportInformationAdditions
            .Where(i => i.IncidentReportId == reportId)
            .OrderBy(i => i.CreatedAt)
            .Select(i => new ReportInformationDto(i.Id, i.Message, i.AttachmentId, i.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReportInformationDto> AddInformationAsync(
        Guid reportId, Guid reporterId, AddReportInformationRequest request, string? requestIp, string? userAgent,
        CancellationToken cancellationToken)
    {
        var report = await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);

        var bucket = ReportStatusProjection.Project(report.VerificationStatus, report.CaseStatus).Bucket;
        if (bucket != ReportListBucket.Active)
        {
            throw new BusinessRuleException(
                "You can only add information to a report while it's still active — this one is already resolved, rejected, or withdrawn.");
        }

        if (request.AttachmentId is { } attachmentId)
        {
            var attachmentExists = await db.IncidentMediaAttachments.AnyAsync(
                a => a.Id == attachmentId && a.IncidentReportId == reportId && !a.IsDeleted, cancellationToken);
            if (!attachmentExists)
            {
                throw new NotFoundException(nameof(IncidentMediaAttachment), attachmentId);
            }
        }

        var addition = new ReportInformationAddition
        {
            Id = Guid.NewGuid(),
            IncidentReportId = reportId,
            ReporterId = reporterId,
            Message = request.Message.Trim(),
            AttachmentId = request.AttachmentId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ReportInformationAdditions.Add(addition);

        await auditLogger.LogAsync(
            adminUserId: null, "ReportInformationAdded", nameof(IncidentReport), reportId.ToString(),
            previousValue: null, newValue: new { AdditionId = addition.Id, addition.AttachmentId },
            requestIp, userAgent, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new ReportInformationDto(addition.Id, addition.Message, addition.AttachmentId, addition.CreatedAt);
    }

    private static readonly CaseStatus[] WithdrawableCaseStatuses =
        [CaseStatus.VerificationPending, CaseStatus.UnderReview, CaseStatus.Assigned];

    public async Task<MobileReportDetailDto> WithdrawAsync(
        Guid reportId, Guid reporterId, WithdrawReportRequest request, string? requestIp, string? userAgent,
        CancellationToken cancellationToken)
    {
        var report = await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);

        if (!WithdrawableCaseStatuses.Contains(report.CaseStatus))
        {
            throw new BusinessRuleException(
                "This report can no longer be withdrawn — an officer has already started active work on it.");
        }

        var previousCaseStatus = report.CaseStatus;
        var now = DateTimeOffset.UtcNow;
        report.CaseStatus = CaseStatus.Withdrawn;
        report.WithdrawnAt = now;
        report.WithdrawalReason = request.Reason.Trim();
        report.UpdatedAt = now;

        // No StatusHistory row: StatusHistory.ChangedByAdminId is a required FK to
        // AdminUser (every other writer is an admin decision/transition), and a reporter
        // withdrawal has no admin actor. The AuditLog row below (adminUserId: null,
        // matching every other reporter-initiated action in this service) plus the
        // report's own WithdrawnAt/WithdrawalReason/CaseStatus fields — already visible
        // on both the mobile and admin report views — fully cover this transition
        // without a nullable FK schema change.
        await visibilityService.RecomputeAsync(report, cancellationToken);

        await auditLogger.LogAsync(
            adminUserId: null, "ReportWithdrawnByReporter", nameof(IncidentReport), reportId.ToString(),
            previousValue: new { PreviousCaseStatus = previousCaseStatus },
            newValue: new { report.WithdrawalReason }, requestIp, userAgent, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        var categoryName = await db.IncidentCategories
            .Where(c => c.Id == report.CategoryId).Select(c => c.Name).FirstAsync(cancellationToken);
        return await BuildDetailDtoAsync(report, categoryName, cancellationToken);
    }

    public async Task<IReadOnlyList<MobileCategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken) =>
        await db.IncidentCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new MobileCategoryDto(c.Id, c.Name, c.Slug, c.IconKey, c.ColourToken, c.DefaultPriority, c.DisplayOrder))
            .ToListAsync(cancellationToken);

    public async Task<DraftDto> CreateDraftAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var draft = new ReportDraft
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.ReportDrafts.Add(draft);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildDraftDtoAsync(draft, categoryName: null, cancellationToken);
    }

    public async Task<DraftDto> UpdateDraftAsync(
        Guid draftId, Guid reporterId, UpdateDraftRequest request, CancellationToken cancellationToken)
    {
        var draft = await FindOwnedDraftOrThrowAsync(draftId, reporterId, cancellationToken);
        EnsureNotSubmitted(draft);

        string? categoryName = null;
        if (request.CategoryId is { } categoryId)
        {
            var category = await db.IncidentCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive, cancellationToken)
                ?? throw new NotFoundException(nameof(IncidentCategory), categoryId);
            draft.CategoryId = category.Id;
            categoryName = category.Name;
        }
        else
        {
            draft.CategoryId = null;
        }

        draft.Description = request.Description?.Trim();
        draft.IncidentOccurredAt = request.IncidentOccurredAt;
        draft.InitialPrioritySignal = request.InitialPrioritySignal;
        draft.LocationDescription = request.LocationDescription?.Trim();
        draft.Latitude = request.Latitude;
        draft.Longitude = request.Longitude;
        draft.Landmark = request.Landmark?.Trim();
        draft.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return await BuildDraftDtoAsync(draft, categoryName, cancellationToken);
    }

    public async Task<MobileReportDetailDto> SubmitDraftAsync(
        Guid draftId, Guid reporterId, SubmitDraftRequest request, string? requestIp, string? userAgent,
        CancellationToken cancellationToken)
    {
        var draft = await db.ReportDrafts
            .Include(d => d.Attachments)
            .FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken);
        if (draft is null || draft.ReporterId != reporterId)
        {
            throw new NotFoundException(nameof(ReportDraft), draftId);
        }

        // Idempotent: a retried submit for an already-submitted draft returns the report
        // created the first time instead of erroring or creating a duplicate case.
        if (draft.SubmittedReportId is { } existingReportId)
        {
            var existingReport = await FindOwnedReportOrThrowAsync(existingReportId, reporterId, cancellationToken);
            var existingCategoryName = await db.IncidentCategories
                .Where(c => c.Id == existingReport.CategoryId).Select(c => c.Name).FirstAsync(cancellationToken);
            return await BuildDetailDtoAsync(existingReport, existingCategoryName, cancellationToken);
        }

        if (!request.TruthDeclarationAccepted)
        {
            throw new BusinessRuleException("You must confirm the truth declaration before submitting.");
        }

        if (draft.CategoryId is null || string.IsNullOrWhiteSpace(draft.Description) || draft.IncidentOccurredAt is null
            || string.IsNullOrWhiteSpace(draft.LocationDescription))
        {
            throw new BusinessRuleException(
                "This report is missing required information — category, description, incident time, and " +
                "location are all required before submitting.");
        }

        var category = await db.IncidentCategories
            .FirstOrDefaultAsync(c => c.Id == draft.CategoryId && c.IsActive, cancellationToken)
            ?? throw new NotFoundException(nameof(IncidentCategory), draft.CategoryId.Value);

        var now = DateTimeOffset.UtcNow;
        var report = new IncidentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            CategoryId = category.Id,
            SourceChannel = SourceChannel.MobileApp,
            Description = draft.Description.Trim(),
            IncidentOccurredAt = draft.IncidentOccurredAt.Value,
            LocationDescription = draft.LocationDescription.Trim(),
            Latitude = draft.Latitude,
            Longitude = draft.Longitude,
            Landmark = draft.Landmark,
            // Server-controlled — see CreateMobileReportRequest's doc comment; the draft's
            // InitialPrioritySignal is context only, recorded below in the audit log.
            Priority = category.DefaultPriority,
            VerificationStatus = VerificationStatus.Pending,
            CaseStatus = CaseStatus.VerificationPending,
            TruthDeclarationAcceptedAt = now,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.IncidentReports.Add(report);

        // Re-parent attachments — the storage objects stay exactly where they already
        // are (draft-scoped paths); only the metadata row moves to the report table.
        foreach (var draftAttachment in draft.Attachments)
        {
            db.IncidentMediaAttachments.Add(new IncidentMediaAttachment
            {
                Id = Guid.NewGuid(),
                IncidentReportId = report.Id,
                FileName = draftAttachment.FileName,
                StoragePath = draftAttachment.StoragePath,
                MediaType = draftAttachment.MediaType,
                MimeType = draftAttachment.MimeType,
                FileSizeBytes = draftAttachment.FileSizeBytes,
                SortOrder = draftAttachment.SortOrder,
                UploadedAt = draftAttachment.UploadedAt,
                UploadedByReporterId = reporterId,
                IsDeleted = false
            });
        }
        db.ReportDraftAttachments.RemoveRange(draft.Attachments);

        draft.SubmittedReportId = report.Id;
        draft.UpdatedAt = now;

        await auditLogger.LogAsync(
            adminUserId: null, "ReportSubmittedViaMobileDraft", nameof(IncidentReport), report.Id.ToString(),
            previousValue: null,
            newValue: new
            {
                report.CategoryId, ReporterId = reporterId, DraftId = draftId,
                draft.InitialPrioritySignal, AttachmentCount = draft.Attachments.Count
            },
            requestIp, userAgent, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await BuildDetailDtoAsync(report, category.Name, cancellationToken);
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

    private async Task<ReportDraft> FindOwnedDraftOrThrowAsync(
        Guid draftId, Guid reporterId, CancellationToken cancellationToken)
    {
        var draft = await db.ReportDrafts.FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken);
        if (draft is null || draft.ReporterId != reporterId)
        {
            throw new NotFoundException(nameof(ReportDraft), draftId);
        }

        return draft;
    }

    private static void EnsureNotSubmitted(ReportDraft draft)
    {
        if (draft.SubmittedReportId is not null)
        {
            throw new BusinessRuleException("This report has already been submitted and can no longer be edited.");
        }
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
            report.Longitude, report.Landmark, report.VerificationStatus, report.CaseStatus, report.Priority,
            report.ResolutionSummary, report.CreatedAt, report.UpdatedAt, report.ClosedAt,
            report.WithdrawnAt, report.WithdrawalReason, statusHistory, attachments);
    }

    private async Task<DraftDto> BuildDraftDtoAsync(ReportDraft draft, string? categoryName, CancellationToken cancellationToken)
    {
        if (categoryName is null && draft.CategoryId is { } categoryId)
        {
            categoryName = await db.IncidentCategories
                .Where(c => c.Id == categoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken);
        }

        var attachments = await db.ReportDraftAttachments
            .Where(a => a.ReportDraftId == draft.Id)
            .OrderBy(a => a.SortOrder)
            .Select(a => new MediaAttachmentDto(a.Id, a.FileName, a.MediaType, a.MimeType, a.FileSizeBytes, a.SortOrder, a.UploadedAt))
            .ToListAsync(cancellationToken);

        return new DraftDto(
            draft.Id, draft.CategoryId, categoryName, draft.Description, draft.IncidentOccurredAt,
            draft.InitialPrioritySignal, draft.LocationDescription, draft.Latitude, draft.Longitude, draft.Landmark,
            draft.SubmittedReportId, draft.CreatedAt, draft.UpdatedAt, attachments);
    }
}
