using BanchoNET.Core.Abstractions.Services;
using StackExchange.Redis;

namespace BanchoNET.Services;

public sealed class AccessTokenDenylist(IConnectionMultiplexer redis) : IAccessTokenDenylist
{
    private readonly IDatabase _redis = redis.GetDatabase();

    public async Task Revoke(
        string jti,
        DateTimeOffset expiresAt
    ) {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero) return;

        await _redis.StringSetAsync(Key(jti), 1, ttl);
    }

    public async Task<bool> IsRevoked(
        string jti
    ) {
        return await _redis.KeyExistsAsync(Key(jti));
    }

    private static string Key(
        string jti
    ) => $"oauth:revoked:{jti}";
}