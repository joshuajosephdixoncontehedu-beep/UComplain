using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Settings;
using CommunityIncidentReporting.Application.Features.Settings.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class SettingsService(AppDbContext db, IAuditLogger auditLogger) : ISettingsService
{
    public async Task<SettingsDto> GetAsync(CancellationToken cancellationToken) =>
        ToDto(await GetOrCreateAsync(cancellationToken));

    public async Task<SettingsDto> UpdateAsync(
        UpdateSettingsRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        var previous = ToDto(settings);

        settings.OrganizationName = request.OrganizationName;
        settings.OrganizationContactEmail = request.OrganizationContactEmail;
        settings.NotifyOnNewVerifiedReport = request.NotifyOnNewVerifiedReport;
        settings.NotifyOnCriticalPriority = request.NotifyOnCriticalPriority;
        settings.DefaultVerificationSlaHours = request.DefaultVerificationSlaHours;
        settings.DuplicateDetectionWindowHours = request.DuplicateDetectionWindowHours;
        settings.ReporterDataRetentionMonths = request.ReporterDataRetentionMonths;
        settings.AuditLogRetentionMonths = request.AuditLogRetentionMonths;
        settings.WhatsAppPlaceholderNote = request.WhatsAppPlaceholderNote;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        settings.UpdatedByAdminId = context.AdminUserId;

        await auditLogger.LogAsync(
            context.AdminUserId, "SettingsUpdated", nameof(SystemSettings), settings.Id.ToString(),
            previous, request, context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    private async Task<SystemSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new SystemSettings
        {
            Id = Guid.NewGuid(),
            OrganizationName = "UComplain",
            OrganizationContactEmail = "operations@ucomplain.com",
            NotifyOnNewVerifiedReport = true,
            NotifyOnCriticalPriority = true,
            DefaultVerificationSlaHours = 24,
            DuplicateDetectionWindowHours = 72,
            ReporterDataRetentionMonths = 24,
            AuditLogRetentionMonths = 60,
            WhatsAppIntegrationEnabled = false,
            WhatsAppPlaceholderNote = "WhatsApp chatbot integration is not yet implemented. See docs/whatsapp-integration-plan.md.",
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.SystemSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);

        return settings;
    }

    private static SettingsDto ToDto(SystemSettings s) => new(
        s.OrganizationName, s.OrganizationContactEmail, s.NotifyOnNewVerifiedReport, s.NotifyOnCriticalPriority,
        s.DefaultVerificationSlaHours, s.DuplicateDetectionWindowHours, s.ReporterDataRetentionMonths,
        s.AuditLogRetentionMonths, s.WhatsAppIntegrationEnabled, s.WhatsAppPlaceholderNote, s.UpdatedAt);
}
