using System.Security.Cryptography;
using System.Text;

namespace Booking.Application.Delivery;

/// <summary>
/// Generation and hashing of per-restaurant webhook secrets. Only the SHA-256 hash is persisted;
/// the plaintext is shown to the owner exactly once at connect/rotate time.
/// </summary>
public static class DeliverySecrets
{
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    public static string Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
