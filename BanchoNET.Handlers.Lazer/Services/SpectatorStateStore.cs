using System.Text.Json;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Lazer.Spectator.Frames;
using BanchoNET.Core.Utils.SignalR;
using MessagePack;
using StackExchange.Redis;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed class SpectatorStateStore(IConnectionMultiplexer redis) : ISpectatorStateStore
{
    private readonly IDatabase _redis = redis.GetDatabase();

    private const string PendingKey = "bancho:lazer:spectator:pending";
    private static string StateKey(long token) => $"bancho:lazer:spectator:{token}";
    private static string FramesKey(long token) => $"bancho:lazer:spectator:{token}:frames";
    private static string ActiveKey(int userId) => $"bancho:lazer:spectator:active:{userId}";
    
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public async Task<ClientSpectatorState?> Get(
        long scoreToken
    ) {
        var json = await _redis.StringGetAsync(StateKey(scoreToken));

        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<ClientSpectatorState>(json.ToString());
    }

    public async Task Store(
        long scoreToken,
        ClientSpectatorState state
    ) {
        await _redis.StringSetAsync(StateKey(scoreToken), JsonSerializer.Serialize(state), Lifetime);
    }

    public async Task Remove(
        long scoreToken
    ) {
        await _redis.KeyDeleteAsync([StateKey(scoreToken), FramesKey(scoreToken)]);
    }

    public async Task<long?> GetActiveToken(
        int userId
    ) {
        var value = await _redis.StringGetAsync(ActiveKey(userId));

        return value.TryParse(out long token) ? token : null;
    }

    public async Task SetActiveToken(
        int userId,
        long scoreToken
    ) {
        await _redis.StringSetAsync(ActiveKey(userId), scoreToken, Lifetime);
    }

    public async Task ClearActiveToken(
        int userId
    ) {
        await _redis.KeyDeleteAsync(ActiveKey(userId));
    }

    public async Task AppendFrames(
        long scoreToken,
        IList<LegacyReplayFrame> frames
    ) {
        if (frames.Count == 0) return;

        var key = FramesKey(scoreToken);

        var payload = frames
            .Select(f => (RedisValue)MessagePackSerializer.Serialize(f, SignalRUnionWorkaroundResolver.Options))
            .ToArray();

        // Batched so a marathon map can't outlive the TTL it keeps refreshing
        var batch = _redis.CreateBatch();
        var push = batch.ListRightPushAsync(key, payload);
        var expire = batch.KeyExpireAsync(key, Lifetime);
        batch.Execute();

        await Task.WhenAll(push, expire);
    }

    public async Task<List<LegacyReplayFrame>> GetFrames(
        long scoreToken
    ) {
        var values = await _redis.ListRangeAsync(FramesKey(scoreToken));

        return values
            .Select(v => MessagePackSerializer.Deserialize<LegacyReplayFrame>(
                (byte[])v!, SignalRUnionWorkaroundResolver.Options))
            .ToList();
    }

    public async Task MarkPending(
        long scoreToken
    ) {
        await _redis.SetAddAsync(PendingKey, scoreToken);
    }

    public async Task ClearPending(
        long scoreToken
    ) {
        await _redis.SetRemoveAsync(PendingKey, scoreToken);
    }

    public async Task<long[]> GetPending()
    {
        var values = await _redis.SetMembersAsync(PendingKey);

        return values.Select(v => (long)v).ToArray();
    }
}