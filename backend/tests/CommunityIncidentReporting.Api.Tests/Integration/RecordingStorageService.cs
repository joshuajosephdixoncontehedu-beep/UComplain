using CommunityIncidentReporting.Application.Common.Interfaces;

namespace CommunityIncidentReporting.Api.Tests.Integration;

/// <summary>
/// Test double for ISupabaseStorageService — keeps uploaded bytes in memory instead of
/// calling a real Supabase project, so integration tests can exercise the full
/// upload/delete/signed-URL flow without live credentials.
/// </summary>
public class RecordingStorageService : ISupabaseStorageService
{
    private readonly Dictionary<string, byte[]> _objects = new();
    private readonly object _lock = new();

    public IReadOnlyDictionary<string, byte[]> Objects
    {
        get { lock (_lock) { return new Dictionary<string, byte[]>(_objects); } }
    }

    public async Task UploadAsync(string storagePath, Stream content, string contentType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        lock (_lock) { _objects[storagePath] = buffer.ToArray(); }
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        lock (_lock) { _objects.Remove(storagePath); }
        return Task.CompletedTask;
    }

    public Task<string> CreateSignedUrlAsync(string storagePath, int expirySeconds, CancellationToken cancellationToken) =>
        Task.FromResult($"https://test.supabase.co/storage/v1/object/sign/{storagePath}?token=fake&expires={expirySeconds}");
}
