using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>Metadata only — never a StoragePath or URL. Use the access-url endpoint for a short-lived signed URL.</summary>
public record MediaAttachmentDto(
    Guid Id,
    string FileName,
    MediaType MediaType,
    string MimeType,
    long FileSizeBytes,
    int SortOrder,
    DateTimeOffset UploadedAt);
