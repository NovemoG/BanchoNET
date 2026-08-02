using System.Text.Json;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api;
using StackExchange.Redis;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed class ScoreTokenStore(IConnectionMultiplexer redis) : IScoreTokenStore
{
    private readonly IDatabase _redis = redis.GetDatabase();

    private const string SequenceKey = "bancho:lazer:score-token:seq";
    private static string TokenKey(long tokenId) => $"bancho:lazer:score-token:{tokenId}";
    
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public async Task<long> NextTokenId()
    {
        return await _redis.StringIncrementAsync(SequenceKey);
    }

    public async Task Store(
        PendingScore pending
    ) {
        await _redis.StringSetAsync(
            TokenKey(pending.Response.Id),
            JsonSerializer.Serialize(pending),
            Lifetime
        );
    }

    public async Task<PendingScore?> Get(
        long tokenId
    ) {
        var json = await _redis.StringGetAsync(TokenKey(tokenId));

        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<PendingScore>(json.ToString());
    }

    public async Task Remove(
        long tokenId
    ) {
        await _redis.KeyDeleteAsync(TokenKey(tokenId));
    }
}