using System.Text;
using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Infrastructure.Storage;

/// <summary>
/// Allowlisted MIME types plus a lightweight magic-byte sniff — validates the actual
/// file content against the claimed Content-Type rather than trusting it (or the
/// filename extension) outright, per the "validate actual file type where practical"
/// requirement. Not exhaustive format detection, just enough to catch a mislabeled or
/// spoofed upload for the small set of types this system accepts.
/// </summary>
public static class MediaTypeDetector
{
    private static readonly Dictionary<string, MediaType> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = MediaType.Image,
        ["image/png"] = MediaType.Image,
        ["image/webp"] = MediaType.Image,
        ["video/mp4"] = MediaType.Video,
        ["video/quicktime"] = MediaType.Video,
        ["audio/mpeg"] = MediaType.Audio,
        ["audio/mp4"] = MediaType.Audio,
        ["audio/wav"] = MediaType.Audio,
        ["application/pdf"] = MediaType.Document
    };

    public static bool TryGetMediaType(string mimeType, out MediaType mediaType) =>
        AllowedMimeTypes.TryGetValue(mimeType, out mediaType);

    /// <summary>header should be at least the first 16 bytes of the file, read before the rest of the stream is consumed.</summary>
    public static bool MatchesDeclaredType(ReadOnlySpan<byte> header, string mimeType) => mimeType.ToLowerInvariant() switch
    {
        "image/jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
        "image/png" => header.Length >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
        "image/webp" => header.Length >= 12 && Riff(header) && Ascii(header[8..12]) == "WEBP",
        "video/mp4" or "video/quicktime" => header.Length >= 8 && Ascii(header[4..8]) == "ftyp",
        "audio/mp4" => header.Length >= 8 && Ascii(header[4..8]) == "ftyp",
        "audio/mpeg" => header.Length >= 3 &&
            ((header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33) || (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)),
        "audio/wav" => header.Length >= 12 && Riff(header) && Ascii(header[8..12]) == "WAVE",
        "application/pdf" => header.Length >= 4 && Ascii(header[..4]) == "%PDF",
        _ => false
    };

    private static bool Riff(ReadOnlySpan<byte> header) => Ascii(header[..4]) == "RIFF";

    private static string Ascii(ReadOnlySpan<byte> bytes) => Encoding.ASCII.GetString(bytes);
}
