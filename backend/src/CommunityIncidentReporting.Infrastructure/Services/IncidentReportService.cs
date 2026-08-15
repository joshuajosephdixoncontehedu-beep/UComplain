using System.Linq.Expressions;
using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Application.Features.Reports;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class IncidentReportService(AppDbContext db, IAuditLogger auditLogger) : IIncidentReportService
{
    // Manual (POST .../status) transitions only. Verification decisions move a report
    // into/out of VerificationPending/Rejected/Duplicate separately (see
    // VerificationService) — those are never reachable through this map, so an admin
    // can't manually "un-reject" a report by calling the status endpoint.
    private static readonly Dictionary<CaseStatus, CaseStatus[]> AllowedTransitions = new()
    {
        [CaseStatus.UnderReview] = [CaseStatus.Assigned, CaseStatus.InProgress],
        [CaseStatus.Assigned] = [CaseStatus.InProgress, CaseStatus.UnderReview],
        [CaseStatus.InProgress] = [CaseStatus.Resolved, CaseStatus.Assigned],
        [CaseStatus.Resolved] = [CaseStatus.Closed, CaseStatus.InProgress]
    };

    public async Task<PagedResult<IncidentReportListItemDto>> GetAllAsync(
        GetReportsQuery query, CancellationToken cancellationToken)
    {
        // The operational queue is always Verified-only — see docs/api-contract.md's
        // verification rules. There is deliberately no way to widen this filter here;
        // non-Verified reports are only reachable through the verification queue.
        var reports = db.IncidentReports.Where(r => r.VerificationStatus == VerificationStatus.Verified);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            reports = reports.Where(r =>
                EF.Functions.ILike(r.CaseReference, $"%{term}%") || EF.Functions.ILike(r.Description, $"%{term}%"));
        }

        if (query.CategoryId is { } categoryId)
        {
            reports = reports.Where(r => r.CategoryId == categoryId);
        }

        if (query.Priority is { } priority)
        {
            reports = reports.Where(r => r.Priority == priority);
        }

        if (query.CaseStatus is { } caseStatus)
        {
            reports = reports.Where(r => r.CaseStatus == caseStatus);
        }

        if (query.AssignedAdminId is { } assignedAdminId)
        {
            reports = reports.Where(r => r.AssignedAdminId == assignedAdminId);
        }

        if (query.SourceChannel is { } sourceChannel)
        {
            reports = reports.Where(r => r.SourceChannel == sourceChannel);
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var term = query.Location.Trim();
            reports = reports.Where(r => EF.Functions.ILike(r.LocationDescription, $"%{term}%"));
        }

        if (query.From is { } from)
        {
            reports = reports.Where(r => r.CreatedAt >= from);
        }

        if (query.To is { } to)
        {
            reports = reports.Where(r => r.CreatedAt <= to);
        }

        var total = await reports.CountAsync(cancellationToken);

        reports = ApplySort(reports, query.SortBy, query.SortDir);

        var items = await reports
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new IncidentReportListItemDto(
                r.Id, r.CaseReference, r.Category!.Name, r.LocationDescription, r.CreatedAt, r.Priority,
                r.VerificationStatus, r.CaseStatus, r.AssignedAdminId, r.AssignedAdmin!.FullName, r.SourceChannel))
            .ToListAsync(cancellationToken);

        return new PagedResult<IncidentReportListItemDto>
        {
            Items = items, Total = total, Page = query.Page, PageSize = query.PageSize
        };
    }

    private static IQueryable<IncidentReport> ApplySort(IQueryable<IncidentReport> query, string? sortBy, string? sortDir)
    {
        // Whitelisted, strongly-typed sort keys — never a raw string passed to
        // OrderBy, so there's no injection surface and everything stays SQL-translatable.
        Expression<Func<IncidentReport, object>> keySelector = sortBy?.ToLowerInvariant() switch
        {
            "priority" => r => r.Priority,
            "casestatus" => r => r.CaseStatus,
            "incidentoccurredat" => r => r.IncidentOccurredAt,
            "location" or "locationdescription" => r => r.LocationDescription,
            _ => r => r.CreatedAt
        };

        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        query = descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
        // Tiebreaker for stable pagination across pages, per EF Core's pagination guidance.
        return descending ? ((IOrderedQueryable<IncidentReport>)query).ThenByDescending(r => r.Id)
            : ((IOrderedQueryable<IncidentReport>)query).ThenBy(r => r.Id);
    }

    public async Task<IncidentReportDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var report = await FindReportOrThrowAsync(id, cancellationToken);
        return await BuildDetailDtoAsync(report, cancellationToken);
    }

    public async Task<IncidentReportDetailDto> UpdateAsync(
        Guid id, UpdateReportRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var report = await FindReportOrThrowAsync(id, cancellationToken);

        var categoryExists = await db.IncidentCategories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException(nameof(IncidentCategory), request.CategoryId);
        }

        var previous = new { report.CategoryId, report.Priority, report.LocationDescription, report.Description };

        report.CategoryId = request.CategoryId;
        report.Priority = request.Priority;
        report.LocationDescription = request.LocationDescription;
        report.Description = request.Description;
        report.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            context.AdminUserId, "ReportUpdated", nameof(IncidentReport), report.Id.ToString(),
            previous, request, context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildDetailDtoAsync(report, cancellationToken);
    }

    public async Task<IncidentReportDetailDto> AssignAsync(
        Guid id, AssignReportRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var report = await FindReportOrThrowAsync(id, cancellationToken);

        var admin = await db.AdminUsers.FindAsync([request.AdminUserId], cancellationToken)
            ?? throw new NotFoundException(nameof(AdminUser), request.AdminUserId);

        var now = DateTimeOffset.UtcNow;

        var openAssignment = await db.ReportAssignments
            .Where(a => a.IncidentReportId == id && a.UnassignedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (openAssignment is not null)
        {
            openAssignment.UnassignedAt = now;
        }

        db.ReportAssignments.Add(new ReportAssignment
        {
            Id = Guid.NewGuid(),
            IncidentReportId = id,
            AdminUserId = admin.Id,
            AssignedByAdminId = context.AdminUserId,
            AssignedAt = now
        });

        var previousAdminId = report.AssignedAdminId;
        report.AssignedAdminId = admin.Id;
        report.UpdatedAt = now;

        if (report.CaseStatus == CaseStatus.UnderReview)
        {
            db.StatusHistories.Add(new StatusHistory
            {
                Id = Guid.NewGuid(),
                IncidentReportId = id,
                PreviousStatus = report.CaseStatus,
                NewStatus = CaseStatus.Assigned,
                ChangedByAdminId = context.AdminUserId,
                Notes = $"Auto-transitioned on assignment to {admin.FullName}.",
                CreatedAt = now
            });
            report.CaseStatus = CaseStatus.Assigned;
        }

        await auditLogger.LogAsync(
            context.AdminUserId, "ReportAssigned", nameof(IncidentReport), report.Id.ToString(),
            new { AssignedAdminId = previousAdminId }, new { AssignedAdminId = admin.Id },
            context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildDetailDtoAsync(report, cancellationToken);
    }

    public async Task<IncidentReportDetailDto> AddNoteAsync(
        Guid id, AddNoteRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var report = await FindReportOrThrowAsync(id, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var note = new InternalNote
        {
            Id = Guid.NewGuid(),
            IncidentReportId = id,
            Content = request.Content,
            CreatedByAdminId = context.AdminUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.InternalNotes.Add(note);

        await auditLogger.LogAsync(
            context.AdminUserId, "ReportNoteAdded", nameof(IncidentReport), report.Id.ToString(),
            previousValue: null, newValue: new { note.Content },
            context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildDetailDtoAsync(report, cancellationToken);
    }

    public async Task<IncidentReportDetailDto> ChangeStatusAsync(
        Guid id, ChangeStatusRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var report = await FindReportOrThrowAsync(id, cancellationToken);

        if (!AllowedTransitions.TryGetValue(report.CaseStatus, out var allowedNextStatuses)
            || !allowedNextStatuses.Contains(request.NewStatus))
        {
            throw new BusinessRuleException(
                $"Cannot transition a report from {report.CaseStatus} to {request.NewStatus}.");
        }

        var now = DateTimeOffset.UtcNow;
        var previousStatus = report.CaseStatus;

        db.StatusHistories.Add(new StatusHistory
        {
            Id = Guid.NewGuid(),
            IncidentReportId = id,
            PreviousStatus = previousStatus,
            NewStatus = request.NewStatus,
            ChangedByAdminId = context.AdminUserId,
            Notes = request.Notes,
            CreatedAt = now
        });

        report.CaseStatus = request.NewStatus;
        report.UpdatedAt = now;
        if (request.NewStatus == CaseStatus.Closed)
        {
            report.ClosedAt = now;
        }
        else if (previousStatus == CaseStatus.Closed)
        {
            report.ClosedAt = null;
        }

        await auditLogger.LogAsync(
            context.AdminUserId, "ReportStatusChanged", nameof(IncidentReport), report.Id.ToString(),
            new { Status = previousStatus.ToString() }, new { Status = request.NewStatus.ToString() },
            context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildDetailDtoAsync(report, cancellationToken);
    }

    private async Task<IncidentReport> FindReportOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await db.IncidentReports.FindAsync([id], cancellationToken)
        ?? throw new NotFoundException(nameof(IncidentReport), id);

    internal async Task<IncidentReportDetailDto> BuildDetailDtoAsync(IncidentReport report, CancellationToken cancellationToken)
    {
        var category = await db.IncidentCategories
            .Where(c => c.Id == report.CategoryId)
            .Select(c => c.Name)
            .FirstAsync(cancellationToken);

        var reporter = await db.Reporters
            .Where(r => r.Id == report.ReporterId)
            .Select(r => new { r.MaskedContactReference, r.IsRestricted })
            .FirstAsync(cancellationToken);

        string? assignedAdminName = report.AssignedAdminId is null
            ? null
            : await db.AdminUsers.Where(a => a.Id == report.AssignedAdminId).Select(a => a.FullName)
                .FirstOrDefaultAsync(cancellationToken);

        var verificationHistory = await db.VerificationEvents
            .Where(v => v.IncidentReportId == report.Id)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VerificationEventDto(
                v.Id, v.VerificationMethod, v.Result, v.AttemptNumber, v.Notes,
                v.PerformedByAdminId, v.PerformedByAdmin != null ? v.PerformedByAdmin.FullName : null, v.CreatedAt))
            .ToListAsync(cancellationToken);

        var statusHistory = await db.StatusHistories
            .Where(s => s.IncidentReportId == report.Id)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StatusHistoryDto(
                s.Id, s.PreviousStatus, s.NewStatus, s.ChangedByAdminId, s.ChangedByAdmin!.FullName, s.Notes, s.CreatedAt))
            .ToListAsync(cancellationToken);

        var notes = await db.InternalNotes
            .Where(n => n.IncidentReportId == report.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new InternalNoteDto(
                n.Id, n.Content, n.CreatedByAdminId, n.CreatedByAdmin!.FullName, n.CreatedAt, n.UpdatedAt))
            .ToListAsync(cancellationToken);

        var assignments = await db.ReportAssignments
            .Where(a => a.IncidentReportId == report.Id)
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new ReportAssignmentDto(
                a.Id, a.AdminUserId, a.AdminUser!.FullName, a.AssignedByAdminId, a.AssignedByAdmin!.FullName,
                a.AssignedAt, a.UnassignedAt))
            .ToListAsync(cancellationToken);

        var attachments = await db.IncidentMediaAttachments
            .Where(a => a.IncidentReportId == report.Id && !a.IsDeleted)
            .OrderBy(a => a.SortOrder)
            .Select(a => new MediaAttachmentDto(a.Id, a.FileName, a.MediaType, a.MimeType, a.FileSizeBytes, a.SortOrder, a.UploadedAt))
            .ToListAsync(cancellationToken);

        var auditEntityId = report.Id.ToString();
        var auditTrail = await db.AuditLogs
            .Where(l => l.EntityType == nameof(IncidentReport) && l.EntityId == auditEntityId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(50)
            .Select(l => new AuditLogEntryDto(
                l.Id, l.AdminUserId, l.AdminUser != null ? l.AdminUser.FullName : null, l.Action,
                l.PreviousValueJson, l.NewValueJson, l.CreatedAt))
            .ToListAsync(cancellationToken);

        return new IncidentReportDetailDto(
            report.Id, report.CaseReference, report.ReporterId, reporter.MaskedContactReference, reporter.IsRestricted,
            report.CategoryId, category, report.SourceChannel, report.Description, report.IncidentOccurredAt,
            report.LocationDescription, report.Latitude, report.Longitude, report.MediaReference,
            report.VerificationStatus, report.CaseStatus, report.Priority, report.AssignedAdminId, assignedAdminName,
            report.ResolutionSummary, report.CreatedAt, report.UpdatedAt, report.ClosedAt,
            verificationHistory, statusHistory, notes, assignments, auditTrail, attachments);
    }
}
