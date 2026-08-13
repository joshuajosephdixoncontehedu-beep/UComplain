using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Reporters;
using CommunityIncidentReporting.Application.Features.Reporters.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class ReporterService(AppDbContext db, IAuditLogger auditLogger) : IReporterService
{
    public async Task<PagedResult<ReporterListItemDto>> GetAllAsync(
        GetReportersQuery query, CancellationToken cancellationToken)
    {
        var reporters = db.Reporters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            reporters = reporters.Where(r => EF.Functions.ILike(r.MaskedContactReference, $"%{term}%"));
        }

        if (query.VerificationStatus is { } status)
        {
            reporters = reporters.Where(r => r.VerificationStatus == status);
        }

        if (query.IsRestricted is { } restricted)
        {
            reporters = reporters.Where(r => r.IsRestricted == restricted);
        }

        var total = await reporters.CountAsync(cancellationToken);

        var items = await reporters
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new ReporterListItemDto(
                r.Id, r.MaskedContactReference, r.VerificationStatus, r.IsRestricted, r.ConsentAt,
                r.IncidentReports.Count, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReporterListItemDto>
        {
            Items = items, Total = total, Page = query.Page, PageSize = query.PageSize
        };
    }

    public async Task<ReporterDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var reporter = await db.Reporters.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(Reporter), id);

        var reports = await db.IncidentReports
            .Where(r => r.ReporterId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReporterReportSummaryDto(
                r.Id, r.CaseReference, r.Category!.Name, r.CaseStatus, r.VerificationStatus, r.CreatedAt))
            .ToListAsync(cancellationToken);

        var verificationHistory = await db.VerificationEvents
            .Where(v => v.ReporterId == id)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new ReporterVerificationEventDto(
                v.Id, v.IncidentReportId, v.IncidentReport!.CaseReference, v.Result, v.Notes, v.CreatedAt))
            .ToListAsync(cancellationToken);

        return ToDetailDto(reporter, reports, verificationHistory);
    }

    public async Task<ReporterDetailDto> RestrictAsync(Guid id, RequestContext context, CancellationToken cancellationToken)
    {
        var reporter = await db.Reporters.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(Reporter), id);

        reporter.IsRestricted = true;
        reporter.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            context.AdminUserId, "ReporterRestricted", nameof(Reporter), reporter.Id.ToString(),
            new { IsRestricted = false }, new { IsRestricted = true },
            context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ReporterDetailDto> UnrestrictAsync(Guid id, RequestContext context, CancellationToken cancellationToken)
    {
        var reporter = await db.Reporters.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(Reporter), id);

        reporter.IsRestricted = false;
        reporter.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            context.AdminUserId, "ReporterUnrestricted", nameof(Reporter), reporter.Id.ToString(),
            new { IsRestricted = true }, new { IsRestricted = false },
            context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private static ReporterDetailDto ToDetailDto(
        Reporter r, IReadOnlyList<ReporterReportSummaryDto> reports,
        IReadOnlyList<ReporterVerificationEventDto> verificationHistory) => new(
        r.Id, r.MaskedContactReference, r.VerificationStatus, r.IsRestricted, r.ConsentAt, r.CreatedAt,
        reports, verificationHistory);
}
