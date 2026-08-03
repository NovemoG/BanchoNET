using System.Text.Json;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Players;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed class LazerPlayerService(
    IConnectionMultiplexer redis,
    IMemoryCache cache
) : ILazerPlayerService
{
    private readonly IDatabase _redis = redis.GetDatabase();

    private const string OnlineKey = "bancho:lazer:online";
    private const string HiddenKey = "bancho:lazer:hidden";
    private static string PlayerKey(int userId) => $"bancho:lazer:player:{userId}";
    private static string LocalKey(int userId) => $"lazer:player:{userId}";
    private static string ActivityKey(int userId) => $"bancho:lazer:activity:{userId}";

    private static readonly TimeSpan PlayerLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan LocalLifetime = TimeSpan.FromSeconds(10);

    public async Task<bool> AddPlayer(
        ApiPlayer player
    ) {
        var added = await _redis.SetAddAsync(OnlineKey, player.Id);

        player.IsOnline = true;
        await Save(new LazerPlayer { Player = player });

        return added;
    }

    public async Task AssignFriends(
        int userId,
        int[] friends
    ) {
        var player = await GetPlayer(userId);
        if (player == null) return;

        player.Friends = friends;
        await Save(player);
    }

    public async Task<bool> RemovePlayer(
        int userId
    ) {
        cache.Remove(LocalKey(userId));

        var removed = await _redis.SetRemoveAsync(OnlineKey, userId);
        await _redis.SetRemoveAsync(HiddenKey, userId);
        await _redis.KeyDeleteAsync(PlayerKey(userId));
        await _redis.KeyDeleteAsync(ActivityKey(userId));

        return removed;
    }

    public async Task RefreshPlayer(
        int userId,
        ApiPlayer fresh
    ) {
        var cached = await GetPlayer(userId);
        if (cached == null) return;

        fresh.IsOnline = cached.Player.IsOnline;

        await Save(new LazerPlayer
        {
            Player = fresh,
            Friends = cached.Friends,
            LastPlayedBeatmapId = cached.LastPlayedBeatmapId,
            LastPlayedBeatmapExitIndex = cached.LastPlayedBeatmapExitIndex
        });
    }

    public async Task<LazerPlayer?> GetPlayer(
        int userId
    ) {
        if (cache.TryGetValue<LazerPlayer>(LocalKey(userId), out var cached) && cached != null)
            return cached;

        var json = await _redis.StringGetAsync(PlayerKey(userId));
        if (json.IsNullOrEmpty) return null;

        var player = JsonSerializer.Deserialize<LazerPlayer>(json.ToString());
        if (player != null)
            cache.Set(LocalKey(userId), player, LocalLifetime);

        return player;
    }

    public async Task<bool> IsOnline(
        int userId
    ) {
        return await _redis.SetContainsAsync(OnlineKey, userId);
    }

    public async Task<HashSet<int>> FilterOnline(
        IReadOnlyCollection<int> userIds
    ) {
        if (userIds.Count == 0) return [];

        var ids = userIds.ToArray();
        var values = ids.Select(id => (RedisValue)id).ToArray();
        var connected = await _redis.SetContainsAsync(OnlineKey, values);
        var hidden = await _redis.SetContainsAsync(HiddenKey, values);

        var online = new HashSet<int>();
        for (var i = 0; i < ids.Length; i++)
        {
            if (connected[i] && !hidden[i])
                online.Add(ids[i]);
        }

        return online;
    }

    public async Task SetLastPlayed(
        int userId,
        int beatmapId,
        int exitIndex
    ) {
        var player = await GetPlayer(userId);
        if (player == null) return;

        player.LastPlayedBeatmapId = beatmapId;
        player.LastPlayedBeatmapExitIndex = exitIndex;

        await Save(player);
    }

    public async Task<HashSet<int>> FilterHidden(
        IReadOnlyCollection<int> userIds
    ) {
        if (userIds.Count == 0) return [];

        var ids = userIds.ToArray();
        var values = ids.Select(id => (RedisValue)id).ToArray();
        var results = await _redis.SetContainsAsync(HiddenKey, values);

        var hidden = new HashSet<int>();
        for (var i = 0; i < ids.Length; i++)
        {
            if (results[i])
                hidden.Add(ids[i]);
        }

        return hidden;
    }

    public async Task SetPresenceHidden(
        int userId,
        bool hidden
    ) {
        if (hidden) await _redis.SetAddAsync(HiddenKey, userId);
        else await _redis.SetRemoveAsync(HiddenKey, userId);
    }

    public async Task<bool> WriteActivity(
        int userId,
        TimeSpan interval
    ) {
        return await _redis.StringSetAsync(ActivityKey(userId), 1, interval, When.NotExists);
    }

    private async Task Save(
        LazerPlayer player
    ) {
        var userId = player.Player.Id;

        await _redis.StringSetAsync(PlayerKey(userId), JsonSerializer.Serialize(player), PlayerLifetime);
        cache.Set(LocalKey(userId), player, LocalLifetime);
    }
}