namespace CommunityIncidentReporting.Infrastructure.Storage;

/// <summary>Per-report attachment limits, bound from the "MediaUpload" config section (MediaUpload__* env vars).</summary>
public class MediaUploadOptions
{
    public const string SectionName = "MediaUpload";

    public int MaxImages { get; set; } = 5;
    public int MaxVideos { get; set; } = 2;
    public int MaxAudioFiles { get; set; } = 1;
    public int MaxDocuments { get; set; } = 3;

    public long MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024;
    public long MaxVideoSizeBytes { get; set; } = 100 * 1024 * 1024;
    public long MaxAudioSizeBytes { get; set; } = 20 * 1024 * 1024;
    public long MaxDocumentSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>How long a reporter/admin-issued attachment signed URL stays valid.</summary>
    public int SignedUrlExpirySeconds { get; set; } = 300;
}
