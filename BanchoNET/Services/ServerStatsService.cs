using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Services;
using StackExchange.Redis;

namespace BanchoNET.Services;

public class ServerStatsService(
	IConnectionMultiplexer redis,
	IPlayerService players,
	IMultiplayerService multiplayer
) : IServerStatsService
{
	private const string HistoryKey = "bancho:stats:online";
	private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

	private readonly IDatabase _redis = redis.GetDatabase();

	public async Task<int> GetOnlineCount()
	{ 
		var stable = players.Players.Where(p => !p.IsBot).Select(p => p.Id);
		var lazer = await _redis.SetMembersAsync("bancho:lazer:online");

		return stable
			.Concat(lazer.Select(value => (int)value))
			.Distinct()
			.Count();
	}

	public int GetActiveGameCount() => multiplayer.Matches.Count();

	public async Task SampleOnlineCount()
	{
		var now = DateTime.UtcNow;
		var timestamp = new DateTimeOffset(now).ToUnixTimeSeconds();
		var count = await GetOnlineCount();
		
		await _redis.SortedSetAddAsync(HistoryKey, $"{timestamp}:{count}", timestamp);
		await _redis.SortedSetRemoveRangeByScoreAsync(
			HistoryKey,
			double.NegativeInfinity,
			new DateTimeOffset(now - Retention).ToUnixTimeSeconds(),
			Exclude.Stop);
	}

	public async Task<List<OnlineSample>> GetOnlineHistory()
	{
		var from = new DateTimeOffset(DateTime.UtcNow - Retention).ToUnixTimeSeconds();
		var entries = await _redis.SortedSetRangeByScoreAsync(HistoryKey, from);

		var samples = new List<OnlineSample>(entries.Length);

		foreach (var entry in entries)
		{
			var parts = ((string?)entry)?.Split(':');

			if (parts is not { Length: 2 }) continue;
			if (!long.TryParse(parts[0], out var timestamp)) continue;
			if (!int.TryParse(parts[1], out var count)) continue;

			samples.Add(new OnlineSample(DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime, count));
		}

		return samples;
	}
}