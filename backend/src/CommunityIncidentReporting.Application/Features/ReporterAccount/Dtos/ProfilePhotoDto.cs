namespace CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;

/// <summary>Null PhotoUrl/ExpiresAt means no photo is set — never a permanent URL, see ISupabaseStorageService.</summary>
public record ProfilePhotoDto(string? PhotoUrl, DateTimeOffset? ExpiresAt);
