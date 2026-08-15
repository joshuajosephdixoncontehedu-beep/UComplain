namespace CommunityIncidentReporting.Application.Common.Exceptions;

/// <summary>
/// The configured object storage provider (Supabase Storage) failed an upload, delete,
/// or signed-URL request. Maps to HTTP 502 Bad Gateway. The message must always be safe
/// to show a client — never include the service role key or a raw provider response body.
/// </summary>
public class StorageException(string message) : Exception(message);
