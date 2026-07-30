using System.Security.Cryptography;
using System.Text;
using BanchoNET.Core.Abstractions.Services;
using Microsoft.Extensions.Caching.Memory;

namespace BanchoNET.Infrastructure.Services;

public sealed class PasswordService(IMemoryCache cache) : IPasswordService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);

    public bool Verify(
        string passwordMD5,
        string passwordHash
    ) {
        if (string.IsNullOrEmpty(passwordMD5) || string.IsNullOrEmpty(passwordHash))
            return false;

        if (cache.TryGetValue<string>(CacheKey(passwordHash), out var verified) && verified != null)
            return FixedTimeEquals(passwordMD5, verified);

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(passwordMD5, passwordHash))
                return false;
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Placeholder hashes (bancho bot, deleted players) aren't valid bcrypt
            return false;
        }

        Store(passwordHash, passwordMD5);
        return true;
    }

    public string Hash(
        string passwordMD5
    ) {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(passwordMD5);
        Store(passwordHash, passwordMD5);

        return passwordHash;
    }

    public void Invalidate(
        string passwordHash
    ) {
        if (!string.IsNullOrEmpty(passwordHash))
            cache.Remove(CacheKey(passwordHash));
    }

    private void Store(
        string passwordHash,
        string passwordMD5
    ) {
        cache.Set(CacheKey(passwordHash), passwordMD5, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheLifetime
        });
    }

    private static bool FixedTimeEquals(
        string left,
        string right
    ) {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right)
        );
    }

    private static string CacheKey(
        string passwordHash
    ) => $"password:{passwordHash}";
}
