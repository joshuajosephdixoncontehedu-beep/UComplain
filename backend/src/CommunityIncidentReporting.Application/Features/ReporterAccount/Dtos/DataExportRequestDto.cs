using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;

/// <summary>
/// downloadUrl/downloadUrlExpiresAt are only populated when status is Completed, and are
/// freshly issued on every read (never a stored URL — see DataExportRequest's doc comment).
/// </summary>
public record DataExportRequestDto(
    Guid Id, DataExportStatus Status, DateTimeOffset RequestedAt, DateTimeOffset? CompletedAt,
    string? DownloadUrl, DateTimeOffset? DownloadUrlExpiresAt, string? FailureReason);
