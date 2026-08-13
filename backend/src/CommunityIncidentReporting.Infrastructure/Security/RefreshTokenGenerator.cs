using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace CommunityIncidentReporting.Infrastructure.Security;

/// <summary>
/// Refresh tokens are high-entropy random data, not user-chosen secrets, so a fast
/// SHA-256 hash (not BCrypt) is the standard, sufficient way to store them — comparing
/// hashes on refresh without ever persisting the raw token.
/// </summary>
public static class RefreshTokenGenerator
{
    public static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64Url.EncodeToString(bytes);
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
