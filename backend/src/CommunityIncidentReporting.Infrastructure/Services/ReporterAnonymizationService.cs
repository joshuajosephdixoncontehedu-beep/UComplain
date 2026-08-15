using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class ReporterAnonymizationService(AppDbContext db) : IReporterAnonymizationService
{
    public async Task AnonymizeAsync(Reporter reporter, CancellationToken cancellationToken)
    {
        if (reporter.AnonymizedAt is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // Mobile-app account fields.
        reporter.FullName = null;
        reporter.Email = null;
        reporter.NormalizedEmail = null;
        reporter.PhoneNumber = null;
        reporter.PasswordHash = null;

        // WhatsApp identity fields — both are required non-null strings, so cleared to
        // "" (their own CLR default for a reporter with no WhatsApp identity), not null.
        reporter.WhatsAppNumberHash = string.Empty;
        reporter.MaskedContactReference = string.Empty;

        reporter.IsActive = false;
        reporter.LanguagePreference = null;
        reporter.AnonymizedAt = now;
        reporter.UpdatedAt = now;

        var activeSessions = await db.ReporterRefreshTokens
            .Where(t => t.ReporterId == reporter.Id && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeSessions)
        {
            token.RevokedAt = now;
        }

        var activeDevices = await db.DeviceTokens
            .Where(d => d.ReporterId == reporter.Id && d.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var device in activeDevices)
        {
            device.RevokedAt = now;
        }
    }
}
