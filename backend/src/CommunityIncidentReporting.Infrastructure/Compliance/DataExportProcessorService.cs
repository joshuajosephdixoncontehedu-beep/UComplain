using System.Text;
using System.Text.Json;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.ReporterAccount;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommunityIncidentReporting.Infrastructure.Compliance;

/// <summary>
/// Builds a reporter's data export as a single JSON document (profile + own reports +
/// attachment metadata + notifications — never other reporters' data, never internal
/// admin-only fields like InternalNotes/AuditLog) and uploads it to the same private
/// Supabase Storage bucket incident media already uses, under a per-reporter path.
/// StoragePath is all that's persisted — see DataExportRequest's doc comment on why no
/// URL is ever stored.
/// </summary>
public class DataExportProcessorService(
    AppDbContext db, ISupabaseStorageService storageService, ILogger<DataExportProcessorService> logger)
    : IDataExportProcessorService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await db.DataExportRequests
            .Where(e => e.Status == DataExportStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var export in pending)
        {
            export.Status = DataExportStatus.Processing;
        }
        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var export in pending)
        {
            try
            {
                var payload = await BuildPayloadAsync(export.ReporterId, cancellationToken);
                var json = JsonSerializer.Serialize(payload, JsonOptions);
                var storagePath = $"data-exports/{export.ReporterId}/{export.Id}/export.json";

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await storageService.UploadAsync(storagePath, stream, "application/json", cancellationToken);

                export.Status = DataExportStatus.Completed;
                export.StoragePath = storagePath;
                export.CompletedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Data export {ExportId} for reporter {ReporterId} failed.", export.Id, export.ReporterId);
                export.Status = DataExportStatus.Failed;
                export.FailureReason = "The export could not be generated. Please try again.";
            }
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return pending.Count;
    }

    private async Task<object> BuildPayloadAsync(Guid reporterId, CancellationToken cancellationToken)
    {
        var reporter = await db.Reporters.FirstAsync(r => r.Id == reporterId, cancellationToken);

        var reports = await db.IncidentReports
            .Where(r => r.ReporterId == reporterId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.CaseReference,
                CategoryName = r.Category!.Name,
                r.Description,
                r.IncidentOccurredAt,
                r.LocationDescription,
                VerificationStatus = r.VerificationStatus.ToString(),
                CaseStatus = r.CaseStatus.ToString(),
                Priority = r.Priority.ToString(),
                r.CreatedAt,
                Attachments = r.MediaAttachments
                    .Where(a => !a.IsDeleted)
                    .Select(a => new { a.Id, a.FileName, MediaType = a.MediaType.ToString(), a.FileSizeBytes, a.UploadedAt })
            })
            .ToListAsync(cancellationToken);

        var notifications = await db.Notifications
            .Where(n => n.ReporterId == reporterId)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new { n.Id, Type = n.Type.ToString(), n.Title, n.Body, n.CreatedAt, n.ReadAt })
            .ToListAsync(cancellationToken);

        return new
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Profile = new
            {
                reporter.Id,
                reporter.FullName,
                reporter.Email,
                reporter.PhoneNumber,
                reporter.LanguagePreference,
                reporter.CreatedAt,
                reporter.LastLoginAt
            },
            Reports = reports,
            Notifications = notifications
        };
    }
}
