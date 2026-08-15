using System.Text.RegularExpressions;
using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileReports;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Services;

public partial class MediaAttachmentService(
    AppDbContext db,
    ISupabaseStorageService storageService,
    IAuditLogger auditLogger,
    IOptions<MediaUploadOptions> options) : IMediaAttachmentService
{
    /// <summary>Reports can only gain/lose attachments before an admin has started working the case.</summary>
    private static readonly CaseStatus[] MutableCaseStatuses = [CaseStatus.VerificationPending, CaseStatus.UnderReview];

    public async Task<IReadOnlyList<MediaAttachmentDto>> UploadAsync(
        Guid reportId, Guid reporterId, IReadOnlyList<MediaUploadFile> files, CancellationToken cancellationToken)
    {
        var report = await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);
        EnsureMutable(report);

        if (files.Count == 0)
        {
            throw new BusinessRuleException("No files were provided.");
        }

        var opts = options.Value;
        var existingCounts = await db.IncidentMediaAttachments
            .Where(a => a.IncidentReportId == reportId && !a.IsDeleted)
            .GroupBy(a => a.MediaType)
            .Select(g => new { MediaType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.MediaType, g => g.Count, cancellationToken);

        var plannedCounts = new Dictionary<MediaType, int>();
        foreach (var kvp in existingCounts)
        {
            plannedCounts[kvp.Key] = kvp.Value;
        }

        var validated = new List<(MediaUploadFile File, MediaType MediaType)>();
        foreach (var file in files)
        {
            if (!MediaTypeDetector.TryGetMediaType(file.ContentType, out var mediaType))
            {
                throw new BusinessRuleException($"File type '{file.ContentType}' is not allowed.");
            }

            var header = new byte[16];
            var bytesRead = await file.Content.ReadAsync(header.AsMemory(0, 16), cancellationToken);
            file.Content.Position = 0;
            if (!MediaTypeDetector.MatchesDeclaredType(header.AsSpan(0, bytesRead), file.ContentType))
            {
                throw new BusinessRuleException($"'{file.FileName}' does not appear to be a valid {file.ContentType} file.");
            }

            var maxSize = MaxSizeFor(mediaType, opts);
            if (file.Length > maxSize)
            {
                throw new BusinessRuleException(
                    $"'{file.FileName}' exceeds the maximum allowed size of {maxSize / (1024 * 1024)}MB for {mediaType} files.");
            }

            plannedCounts[mediaType] = plannedCounts.GetValueOrDefault(mediaType) + 1;
            validated.Add((file, mediaType));
        }

        foreach (var (mediaType, limit) in new[]
        {
            (MediaType.Image, opts.MaxImages), (MediaType.Video, opts.MaxVideos),
            (MediaType.Audio, opts.MaxAudioFiles), (MediaType.Document, opts.MaxDocuments)
        })
        {
            if (plannedCounts.GetValueOrDefault(mediaType) > limit)
            {
                throw new BusinessRuleException($"A report may have at most {limit} {mediaType} attachment(s).");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var existingMaxSortOrder = await db.IncidentMediaAttachments
            .Where(a => a.IncidentReportId == reportId)
            .Select(a => (int?)a.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var uploadedPaths = new List<string>();
        var newAttachments = new List<IncidentMediaAttachment>();
        try
        {
            var sortOrder = existingMaxSortOrder + 1;
            foreach (var (file, mediaType) in validated)
            {
                var attachmentId = Guid.NewGuid();
                var safeFileName = SanitizeFileName(file.FileName);
                var storagePath = $"incident-reports/{reportId}/{attachmentId}/{safeFileName}";

                await storageService.UploadAsync(storagePath, file.Content, file.ContentType, cancellationToken);
                uploadedPaths.Add(storagePath);

                newAttachments.Add(new IncidentMediaAttachment
                {
                    Id = attachmentId,
                    IncidentReportId = reportId,
                    FileName = safeFileName,
                    StoragePath = storagePath,
                    MediaType = mediaType,
                    MimeType = file.ContentType,
                    FileSizeBytes = file.Length,
                    SortOrder = sortOrder++,
                    UploadedAt = now,
                    UploadedByReporterId = reporterId,
                    IsDeleted = false
                });
            }
        }
        catch
        {
            // Roll back any files this batch already stored — metadata is only written
            // after every file in the batch has uploaded successfully.
            foreach (var path in uploadedPaths)
            {
                await storageService.DeleteAsync(path, cancellationToken);
            }
            throw;
        }

        db.IncidentMediaAttachments.AddRange(newAttachments);

        await auditLogger.LogAsync(
            adminUserId: null, "ReportAttachmentsUploaded", nameof(IncidentReport), reportId.ToString(),
            previousValue: null,
            newValue: new { Count = newAttachments.Count, MediaTypes = newAttachments.Select(a => a.MediaType.ToString()) },
            ipAddress: null, userAgent: null, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return newAttachments
            .Select(a => new MediaAttachmentDto(a.Id, a.FileName, a.MediaType, a.MimeType, a.FileSizeBytes, a.SortOrder, a.UploadedAt))
            .ToList();
    }

    public async Task DeleteAsync(Guid reportId, Guid attachmentId, Guid reporterId, CancellationToken cancellationToken)
    {
        var report = await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);
        EnsureMutable(report);

        var attachment = await db.IncidentMediaAttachments.FirstOrDefaultAsync(
            a => a.Id == attachmentId && a.IncidentReportId == reportId && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(IncidentMediaAttachment), attachmentId);

        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTimeOffset.UtcNow;

        await storageService.DeleteAsync(attachment.StoragePath, cancellationToken);

        await auditLogger.LogAsync(
            adminUserId: null, "ReportAttachmentDeleted", nameof(IncidentReport), reportId.ToString(),
            previousValue: null, newValue: new { AttachmentId = attachmentId }, ipAddress: null, userAgent: null,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SignedUrlResponse> GetReporterAccessUrlAsync(
        Guid reportId, Guid attachmentId, Guid reporterId, CancellationToken cancellationToken)
    {
        await FindOwnedReportOrThrowAsync(reportId, reporterId, cancellationToken);
        return await CreateSignedUrlResponseAsync(reportId, attachmentId, cancellationToken);
    }

    public async Task<SignedUrlResponse> GetAdminAccessUrlAsync(
        Guid reportId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var reportExists = await db.IncidentReports.AnyAsync(r => r.Id == reportId, cancellationToken);
        if (!reportExists)
        {
            throw new NotFoundException(nameof(IncidentReport), reportId);
        }

        return await CreateSignedUrlResponseAsync(reportId, attachmentId, cancellationToken);
    }

    private async Task<SignedUrlResponse> CreateSignedUrlResponseAsync(
        Guid reportId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await db.IncidentMediaAttachments.FirstOrDefaultAsync(
            a => a.Id == attachmentId && a.IncidentReportId == reportId && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(IncidentMediaAttachment), attachmentId);

        var expirySeconds = options.Value.SignedUrlExpirySeconds;
        var url = await storageService.CreateSignedUrlAsync(attachment.StoragePath, expirySeconds, cancellationToken);
        return new SignedUrlResponse(url, DateTimeOffset.UtcNow.AddSeconds(expirySeconds));
    }

    private async Task<IncidentReport> FindOwnedReportOrThrowAsync(
        Guid reportId, Guid reporterId, CancellationToken cancellationToken)
    {
        var report = await db.IncidentReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report is null || report.ReporterId != reporterId)
        {
            throw new NotFoundException(nameof(IncidentReport), reportId);
        }

        return report;
    }

    private static void EnsureMutable(IncidentReport report)
    {
        if (!MutableCaseStatuses.Contains(report.CaseStatus))
        {
            throw new BusinessRuleException(
                "Attachments can only be added or removed while a report is awaiting verification or under review.");
        }
    }

    private static long MaxSizeFor(MediaType mediaType, MediaUploadOptions options) => mediaType switch
    {
        MediaType.Image => options.MaxImageSizeBytes,
        MediaType.Video => options.MaxVideoSizeBytes,
        MediaType.Audio => options.MaxAudioSizeBytes,
        MediaType.Document => options.MaxDocumentSizeBytes,
        _ => options.MaxDocumentSizeBytes
    };

    private static string SanitizeFileName(string fileName)
    {
        var name = System.IO.Path.GetFileName(fileName);
        name = InvalidFileNameCharsRegex().Replace(name, "_");
        return string.IsNullOrWhiteSpace(name) ? "file" : name;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_.\-]")]
    private static partial Regex InvalidFileNameCharsRegex();
}
