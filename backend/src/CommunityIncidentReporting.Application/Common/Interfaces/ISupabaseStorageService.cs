namespace CommunityIncidentReporting.Application.Common.Interfaces;

/// <summary>
/// Backend-only access to the private Supabase Storage bucket used for incident media.
/// Nothing here ever returns a permanent public URL — CreateSignedUrlAsync issues a
/// short-lived one, freshly, on every call. Throws StorageException on any provider
/// failure (mapped to HTTP 502 by GlobalExceptionHandler).
/// </summary>
public interface ISupabaseStorageService
{
    /// <summary>Uploads to the given object path (e.g. "incident-reports/{reportId}/{attachmentId}/{fileName}").</summary>
    Task UploadAsync(string storagePath, Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>Best-effort by design — callers should log and continue rather than fail a request over cleanup.</summary>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);

    Task<string> CreateSignedUrlAsync(string storagePath, int expirySeconds, CancellationToken cancellationToken);
}
