using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports;
using CommunityIncidentReporting.Application.Features.PublicMap;
using CommunityIncidentReporting.Application.Features.ReporterAccount;
using CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Compliance;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class ReporterAccountService(
    AppDbContext db, IAuditLogger auditLogger, IMobileReportService mobileReportService,
    ISupabaseStorageService storageService, IOptions<ComplianceOptions> options) : IReporterAccountService
{
    public async Task<ReporterPrivacySettingDto> GetPrivacyAsync(Guid reporterId, CancellationToken cancellationToken) =>
        ToDto(await GetOrCreatePrivacyAsync(reporterId, cancellationToken));

    public async Task<ReporterPrivacySettingDto> UpdatePrivacyAsync(
        Guid reporterId, UpdateReporterPrivacySettingRequest request, CancellationToken cancellationToken)
    {
        var privacy = await GetOrCreatePrivacyAsync(reporterId, cancellationToken);

        privacy.UsePreciseLocation = request.UsePreciseLocation;
        privacy.ShowOnPublicMap = request.ShowOnPublicMap;
        privacy.AllowResponderContact = request.AllowResponderContact;
        privacy.UpdatedAt = DateTimeOffset.UtcNow;

        // Recompute for every one of the caller's own existing reports, not just future
        // ones — a report submitted while ShowOnPublicMap was true must drop off the
        // public map immediately if the reporter turns it off, not just at its next
        // unrelated status change. The new value is already known here, so this computes
        // it directly rather than going through IReportVisibilityService (which would
        // re-query the privacy row we just updated, once per report).
        var reports = await db.IncidentReports.Where(r => r.ReporterId == reporterId).ToListAsync(cancellationToken);
        foreach (var report in reports)
        {
            report.IsPubliclyVisible = ReportPublicVisibility.Compute(
                report.VerificationStatus, report.CaseStatus, request.ShowOnPublicMap);
        }

        await auditLogger.LogAsync(
            adminUserId: null, "ReporterPrivacySettingUpdated", nameof(ReporterPrivacySetting), privacy.Id.ToString(),
            previousValue: null, newValue: request, ipAddress: null, userAgent: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(privacy);
    }

    public async Task<ReporterStatsDto> GetStatsAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var reporter = await db.Reporters.FindAsync([reporterId], cancellationToken)
            ?? throw new NotFoundException(nameof(Reporter), reporterId);

        var counts = await mobileReportService.GetMyReportCountsAsync(reporterId, cancellationToken);

        return new ReporterStatsDto(counts.Active, counts.Resolved, counts.Rejected, counts.Total, reporter.CreatedAt);
    }

    public async Task<ReporterProfileDto> UpdateProfileAsync(
        Guid reporterId, UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var reporter = await db.Reporters.FindAsync([reporterId], cancellationToken)
            ?? throw new NotFoundException(nameof(Reporter), reporterId);

        var previous = new { reporter.FullName, reporter.LanguagePreference };

        reporter.FullName = request.FullName.Trim();
        reporter.LanguagePreference = request.LanguagePreference?.Trim();
        reporter.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            adminUserId: null, "ReporterProfileUpdated", nameof(Reporter), reporter.Id.ToString(),
            previous, request, ipAddress: null, userAgent: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new ReporterProfileDto(
            reporter.Id, reporter.FullName ?? string.Empty, reporter.Email ?? string.Empty, reporter.PhoneNumber,
            reporter.EmailVerifiedAt is not null, reporter.IsActive, reporter.IsRestricted, reporter.LastLoginAt,
            reporter.CreatedAt, reporter.LanguagePreference);
    }

    public async Task<DataExportRequestDto> RequestDataExportAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var existing = await db.DataExportRequests
            .Where(e => e.ReporterId == reporterId
                        && (e.Status == DataExportStatus.Pending || e.Status == DataExportStatus.Processing))
            .OrderByDescending(e => e.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return await ToDtoAsync(existing, cancellationToken);
        }

        var export = new DataExportRequest
        {
            Id = Guid.NewGuid(), ReporterId = reporterId, Status = DataExportStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow
        };
        db.DataExportRequests.Add(export);

        await auditLogger.LogAsync(
            adminUserId: null, "ReporterDataExportRequested", nameof(Reporter), reporterId.ToString(),
            previousValue: null, newValue: new { export.Id }, ipAddress: null, userAgent: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(export, cancellationToken);
    }

    public async Task<DataExportRequestDto> GetLatestDataExportAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var latest = await db.DataExportRequests
            .Where(e => e.ReporterId == reporterId)
            .OrderByDescending(e => e.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(DataExportRequest), reporterId);

        return await ToDtoAsync(latest, cancellationToken);
    }

    public async Task<AccountDeletionRequestDto> RequestAccountDeletionAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var existing = await db.AccountDeletionRequests
            .Where(d => d.ReporterId == reporterId && d.Status == AccountDeletionStatus.Pending)
            .OrderByDescending(d => d.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return ToDto(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var request = new AccountDeletionRequest
        {
            Id = Guid.NewGuid(), ReporterId = reporterId, Status = AccountDeletionStatus.Pending,
            RequestedAt = now, ScheduledForAt = now.AddDays(options.Value.AccountDeletionGracePeriodDays)
        };
        db.AccountDeletionRequests.Add(request);

        await auditLogger.LogAsync(
            adminUserId: null, "ReporterAccountDeletionRequested", nameof(Reporter), reporterId.ToString(),
            previousValue: null, newValue: new { request.Id, request.ScheduledForAt },
            ipAddress: null, userAgent: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(request);
    }

    public async Task<AccountDeletionRequestDto> CancelAccountDeletionAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var pending = await db.AccountDeletionRequests
            .Where(d => d.ReporterId == reporterId && d.Status == AccountDeletionStatus.Pending)
            .OrderByDescending(d => d.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleException("You don't have a pending account deletion request to cancel.");

        pending.Status = AccountDeletionStatus.Cancelled;
        pending.CancelledAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            adminUserId: null, "ReporterAccountDeletionCancelled", nameof(Reporter), reporterId.ToString(),
            previousValue: null, newValue: new { pending.Id }, ipAddress: null, userAgent: null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(pending);
    }

    private async Task<ReporterPrivacySetting> GetOrCreatePrivacyAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var privacy = await db.ReporterPrivacySettings.FirstOrDefaultAsync(p => p.ReporterId == reporterId, cancellationToken);
        if (privacy is not null)
        {
            return privacy;
        }

        privacy = new ReporterPrivacySetting { Id = Guid.NewGuid(), ReporterId = reporterId, UpdatedAt = DateTimeOffset.UtcNow };
        db.ReporterPrivacySettings.Add(privacy);
        await db.SaveChangesAsync(cancellationToken);
        return privacy;
    }

    private async Task<DataExportRequestDto> ToDtoAsync(DataExportRequest export, CancellationToken cancellationToken)
    {
        if (export.Status != DataExportStatus.Completed || export.StoragePath is null)
        {
            return new DataExportRequestDto(
                export.Id, export.Status, export.RequestedAt, export.CompletedAt, null, null, export.FailureReason);
        }

        var expirySeconds = options.Value.DataExportSignedUrlExpirySeconds;
        var url = await storageService.CreateSignedUrlAsync(export.StoragePath, expirySeconds, cancellationToken);
        return new DataExportRequestDto(
            export.Id, export.Status, export.RequestedAt, export.CompletedAt, url,
            DateTimeOffset.UtcNow.AddSeconds(expirySeconds), export.FailureReason);
    }

    private static ReporterPrivacySettingDto ToDto(ReporterPrivacySetting p) =>
        new(p.UsePreciseLocation, p.ShowOnPublicMap, p.AllowResponderContact, p.UpdatedAt);

    private static AccountDeletionRequestDto ToDto(AccountDeletionRequest d) =>
        new(d.Id, d.Status, d.RequestedAt, d.ScheduledForAt, d.CancelledAt);
}
