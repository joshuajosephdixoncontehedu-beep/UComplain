using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Services;

/// <summary>
/// Talks to Supabase Storage's REST API directly (no SDK — see
/// docs/mobile-client-backend-extension.md) using the service-role key, which is
/// backend-only and never leaves this process. Every path is scoped under the single
/// configured private bucket.
/// </summary>
public class SupabaseStorageService(
    IHttpClientFactory httpClientFactory,
    IOptions<SupabaseStorageOptions> options,
    ILogger<SupabaseStorageService> logger) : ISupabaseStorageService
{
    private const string ClientName = "Supabase";

    public async Task UploadAsync(string storagePath, Stream content, string contentType, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var client = httpClientFactory.CreateClient(ClientName);

        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(ObjectUrl(storagePath), streamContent, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Supabase Storage upload failed (network error) for a media attachment.");
            throw new StorageException("Could not reach the storage provider. Please try again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Supabase Storage upload returned {StatusCode}.", (int)response.StatusCode);
            throw new StorageException("The storage provider rejected the upload. Please try again.");
        }
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var client = httpClientFactory.CreateClient(ClientName);

        try
        {
            var response = await client.DeleteAsync(ObjectUrl(storagePath), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Supabase Storage delete returned {StatusCode} — the object may be orphaned; this does not fail the caller's request.",
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Deleting storage objects is always best-effort — see ISupabaseStorageService.
            logger.LogWarning(ex, "Supabase Storage delete failed — the object may be orphaned.");
        }
    }

    public async Task<string> CreateSignedUrlAsync(string storagePath, int expirySeconds, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var client = httpClientFactory.CreateClient(ClientName);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync(
                $"object/sign/{options.Value.StorageBucket}/{storagePath}", new { expiresIn = expirySeconds }, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Supabase Storage sign request failed (network error).");
            throw new StorageException("Could not reach the storage provider. Please try again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Supabase Storage sign request returned {StatusCode}.", (int)response.StatusCode);
            throw new StorageException("The storage provider could not produce an access link. Please try again.");
        }

        var payload = await response.Content.ReadFromJsonAsync<SignedUrlPayload>(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(payload?.SignedURL))
        {
            throw new StorageException("The storage provider returned an unexpected response.");
        }

        // signedURL comes back as a path relative to the storage API root (e.g.
        // "/object/sign/<bucket>/<path>?token=..."), not an absolute URL.
        return $"{options.Value.Url.TrimEnd('/')}/storage/v1{payload.SignedURL}";
    }

    private string ObjectUrl(string storagePath) => $"object/{options.Value.StorageBucket}/{storagePath}";

    private void EnsureConfigured()
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.Url) || string.IsNullOrWhiteSpace(opts.ServiceRoleKey)
            || string.IsNullOrWhiteSpace(opts.StorageBucket))
        {
            logger.LogError(
                "Supabase Storage is not fully configured (SUPABASE_URL / SUPABASE_SERVICE_ROLE_KEY / SUPABASE_STORAGE_BUCKET).");
            throw new StorageException("Media storage is not configured. Please contact support.");
        }
    }

    private record SignedUrlPayload(string? SignedURL);
}
