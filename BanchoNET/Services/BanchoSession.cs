using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services;

public sealed class BanchoSession : IBanchoSession
{
	#region Beatmaps

	private readonly ConcurrentDictionary<int, BeatmapSet> _beatmapSetsCache = []; // setId -> BeatmapSet
	private readonly ConcurrentDictionary<string, Beatmap> _beatmapMD5Cache = []; // MD5 -> Beatmap
	private readonly ConcurrentDictionary<int, Beatmap> _beatmapIdCache = []; // mapId -> Beatmap
	private readonly List<string> _notSubmittedBeatmaps = []; // MD5s
	private readonly List<string> _needUpdateBeatmaps = []; // MD5s

	#endregion

	#region Other
	
	private readonly ConcurrentDictionary<string, string> _passwordHashes = [];

	#endregion

	public void ClearPasswordsCache() => _passwordHashes.Clear();
	
	public void InsertPasswordHash(string passwordMD5, string passwordHash)
	{
		_passwordHashes.TryAdd(passwordHash, passwordMD5);
	}
	
	public bool CheckHashes(string passwordMD5, string passwordHash)
	{
		if (_passwordHashes.TryGetValue(passwordHash, out var md5))
			return md5 == passwordMD5;

		if (!BCrypt.Net.BCrypt.Verify(passwordMD5, passwordHash))
			return false;
		
		_passwordHashes.TryAdd(passwordHash, passwordMD5);
		return true;
	}
	
	public Beatmap? GetBeatmapByMD5(string beatmapMD5)
	{
		return _beatmapMD5Cache.TryGetValue(beatmapMD5, out var cachedBeatmap) ? cachedBeatmap : null;
	}
	
	public Beatmap? GetBeatmapById(int mapId)
	{
		return _beatmapIdCache.TryGetValue(mapId, out var cachedBeatmap) ? cachedBeatmap : null;
	}
	
	public BeatmapSet? GetBeatmapSet(int setId)
	{
		return _beatmapSetsCache.TryGetValue(setId, out var beatmapSet) ? beatmapSet : null;
	}

	public void CacheBeatmapSet(BeatmapSet set)
	{
		Logger.Shared.LogDebug($"Caching beatmap set with id: {set.Id}", nameof(BanchoSession));
		
		_beatmapSetsCache.AddOrUpdate(set.Id, set, (_, _) => set);

		foreach (var beatmap in set.Beatmaps)
		{
			_beatmapMD5Cache.AddOrUpdate(beatmap.MD5, beatmap, (_, _) => beatmap);
			_beatmapIdCache.AddOrUpdate(beatmap.MapId, beatmap, (_, _) => beatmap);
		}
	}

	public bool IsBeatmapNotSubmitted(string beatmapMD5)
	{
		return _notSubmittedBeatmaps.Contains(beatmapMD5);
	}
	
	public void CacheNotSubmittedBeatmap(string beatmapMD5)
	{
		Logger.Shared.LogDebug($"Caching not submitted beatmap with MD5: {beatmapMD5}", nameof(BanchoSession));
		
		_notSubmittedBeatmaps.Add(beatmapMD5);
	}
	
	public bool BeatmapNeedsUpdate(string beatmapMD5)
	{
		return _needUpdateBeatmaps.Contains(beatmapMD5);
	}
	
	public void CacheNeedsUpdateBeatmap(string beatmapMD5)
	{
		Logger.Shared.LogDebug($"Caching beatmap which needs update with MD5: {beatmapMD5}", nameof(BanchoSession));
		
		_needUpdateBeatmaps.Add(beatmapMD5);
	}
}