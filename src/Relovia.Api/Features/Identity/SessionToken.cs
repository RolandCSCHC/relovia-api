using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Relovia.Api.Features.Identity;

public static class SessionToken
{
    public const int Size = 32;

    public static string Create()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(Size));
    }

    public static byte[]? TryHash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        byte[] raw;
        try
        {
            raw = WebEncoders.Base64UrlDecode(token);
        }
        catch (FormatException)
        {
            return null;
        }

        if (raw.Length != Size)
        {
            return null;
        }

        return SHA256.HashData(raw);
    }
}
