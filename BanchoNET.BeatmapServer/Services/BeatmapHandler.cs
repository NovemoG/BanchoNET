using System.Text.Json;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;

namespace BanchoNET.BeatmapServer.Services;

public sealed class BeatmapHandler(
	ILogger logger,
	IBeatmapService beatmapCache,
	IBeatmapsRepository beatmaps,
	HttpClient httpClient,
	IHttpClientFactory httpClientFactory
) : IBeatmapHandler
{
	private readonly HttpClient _bearerClient = httpClientFactory.CreateClient(nameof(BeatmapHandler));
	
	public async Task<List<ApiBeatmapsetFull>> GetRandomBeatmaps() {
		return (await beatmaps.GetRandomBeatmaps()).Select(bs => new ApiBeatmapsetFull(bs)).ToList();
	}
	
	public async Task<bool> CheckIfMapExistsOnBanchoByFilename(
		string filename
	) {
		var response = await httpClient.GetAsync($"https://osu.ppy.sh/web/maps/{filename}");
		return response.Content.Headers.ContentLength > 0;
	}

	public async Task<bool> EnsureLocalBeatmapFile(
		int beatmapId,
		string beatmapMD5
	) {
		var beatmapPath = Storage.GetBeatmapPath(beatmapId);

		if (!File.Exists(beatmapPath)
		    || !beatmapPath.CheckLocalBeatmapMD5(beatmapMD5))
		{
			var response = await httpClient.GetAsync($"https://old.ppy.sh/osu/{beatmapId}");
			if (response.Content.Headers.ContentLength == 0)
				return false;
			
			logger.LogInfo($"Caching {beatmapId}.osu beatmap file");
			
			await using var fileStream = new FileStream(beatmapPath, FileMode.Create, FileAccess.ReadWrite);
			await response.Content.CopyToAsync(fileStream);
		}
		
		return true;
	}

	public async Task FetchPlayerPlaycount(
		ApiBeatmapsetFull beatmapset,
		int playerId
	) {
		var plays = await beatmaps.FetchPlayerPlaycount(beatmapset.Id, playerId);
		foreach (var play in plays)
			beatmapset.Beatmaps.First(b => b.Id == play.BeatmapId).CurrentUserPlaycount = play.Plays;
	}

	public async Task FetchPlayerFavorited(
		ApiBeatmapsetFull beatmapset,
		int playerId
	) {
		beatmapset.HasFavourited = await beatmaps.FetchPlayerFavorited(beatmapset.Id, playerId);
	}

	public async Task<Beatmap?> GetBeatmap(
		int mapId,
		int setId = -1
	) {
		var beatmap = beatmapCache.GetBeatmap(mapId);
		if (beatmap != null && !beatmap.ShouldRecheckApi())
			return beatmap;

		if (setId < 1)
		{
			var dbBeatmap = await beatmaps.GetBeatmap(mapId);
			if (dbBeatmap != null)
				setId = dbBeatmap.SetId;
			else
			{
				var beatmapId = await GetBeatmapFromApi(mapId);
				if (beatmapId == 0) return null;
				
				// we got the beatmap from api and cached it, retrieve here
				return beatmapCache.GetBeatmap(mapId);
			}
		}

		var beatmapSet = await GetBeatmapset(setId, mapId, recheckApi: true);

		return beatmapSet != null
			? beatmapSet.Beatmaps.FirstOrDefault(b => b.Id == mapId)
			: beatmap;
	}

	public async Task<Beatmap?> GetBeatmap(
		string beatmapMD5,
		int setId = -1
	) {
		var beatmap = beatmapCache.GetBeatmap(beatmapMD5);
		if (beatmap != null && !beatmap.ShouldRecheckApi())
			return beatmap;
		
		var mapId = beatmap?.Id;
		if (setId < 1)
		{
			var dbBeatmap = await beatmaps.GetBeatmap(beatmapMD5);
			if (dbBeatmap != null)
			{
				setId = dbBeatmap.SetId;
				mapId = dbBeatmap.Id;
			}
			else
			{
				var apiMap = await GetBeatmapFromApi(beatmapMD5);
				if (apiMap == null) return null;

				setId = apiMap.BeatmapsetId;
				mapId = apiMap.Id;
			}
		}

		var beatmapset = await GetBeatmapset(setId, mapId ?? 0, recheckApi: true);
		
		return beatmapset != null
			? beatmapset.Beatmaps.FirstOrDefault(b => b.Checksum == beatmapMD5)
			: beatmap;
	}

	public async Task<Beatmapset?> GetBeatmapset(
		int setId,
		int mapId = -1,
		bool recheckApi = false
	) {
		var didApiRequest = false;
		var beatmapset = beatmapCache.GetBeatmapset(setId);
		
		if (beatmapset == null)
		{
			var dbBeatmapset = await beatmaps.GetBeatmapset(setId);
			if (dbBeatmapset == null)
			{
				var apiBeatmapset = await GetBeatmapsetFromApi(setId);
				if (apiBeatmapset == null) return null;

				didApiRequest = true;
				await beatmaps.InsertBeatmapset(beatmapset = new Beatmapset(apiBeatmapset));
			}
			else beatmapset = new Beatmapset(dbBeatmapset);
			
			if (didApiRequest || !recheckApi)
				beatmapCache.InsertBeatmapset(beatmapset);
		}
		
		if (!didApiRequest && mapId > 0)
			if (recheckApi || beatmapset.Beatmaps.All(b => b.Id != mapId))
				await GetBeatmapsetFromApi(setId);
		
		return beatmapset;
	}

	public async Task<List<Beatmap>> GetBeatmaps(
		int[] beatmapIds
	) {
		List<Beatmap> beatmapList = [];
		foreach (var id in beatmapIds)
		{
			var beatmap = await GetBeatmap(id);
			if (beatmap == null) continue;
			
			beatmapList.Add(beatmap);
		}

		return beatmapList;
	}
	
	//TODO allow not providing client_id/secret and use osu.direct's api v2

	private async Task<ApiBeatmap?> GetBeatmapFromApi(
		string checksum,
		string filename
	) {
		var url = $"https://osu.ppy.sh/api/v2/beatmaps/lookup?checksum={checksum}&filename={filename}";
		
		var response = await _bearerClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();
		
		if (response.IsSuccessStatusCode && content.IsValidResponse())
			return Deserialize<ApiBeatmap>(content);
		
		return null;
	}

	private async Task<ApiBeatmap?> GetBeatmapFromApi(
		string checksum
	) {
		var url = $"https://osu.ppy.sh/api/v2/beatmaps/lookup?checksum={checksum}";
		
		var response = await _bearerClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();
		
		if (response.IsSuccessStatusCode && content.IsValidResponse())
			return Deserialize<ApiBeatmap>(content);
		
		return null;
	}

	private async Task<int> GetBeatmapFromApi(
		int beatmapId
	) {
		return (await GetBeatmapsetByMapIdFromApi(beatmapId)) != null
			? beatmapId
			: 0;
	}

	public async Task<ApiBeatmapsetFull?> GetBeatmapsetFromApiOrCached(
		int beatmapsetId,
		bool withAllData = true
	) {
		var cached = beatmapCache.GetBeatmapset(beatmapsetId);
		if (cached != null && !cached.ShouldRecheckApi()) // if it is cached, it must be in db
			return withAllData
				? new ApiBeatmapsetFull((await beatmaps.GetBeatmapsetFull(beatmapsetId))!)
				: new ApiBeatmapsetFull(cached);
		
		return await GetBeatmapsetFromApi(beatmapsetId);
	}

	public async Task<ApiBeatmapsetFull?> GetBeatmapsetByMapIdFromApiOrCached(
		int mapId,
		bool withAllData = true
	) {
		var cached = beatmapCache.GetBeatmap(mapId);
		if (cached != null && !cached.ShouldRecheckApi()) // if it is cached, it must be in db
			return withAllData
				? new ApiBeatmapsetFull((await beatmaps.GetBeatmapsetFull(cached.Set.Id))!)
				: new ApiBeatmapsetFull(cached.Set);

		return await GetBeatmapsetByMapIdFromApi(mapId);
	}

	private async Task<ApiBeatmapsetFull?> GetBeatmapsetByMapIdFromApi(
		int mapId
	) {
		var url = $"https://osu.ppy.sh/api/v2/beatmapsets/lookup?beatmap_id={mapId}";
		
		var response = await _bearerClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode && content.IsValidResponse())
		{
			var beatmapset = Deserialize<ApiBeatmapsetFull>(content);
			if (beatmapset == null) return null;
			
			await UpdateBeatmapset(beatmapset);
			
			return beatmapset;
		}
		
		return null;
	}

	private async Task<ApiBeatmapsetFull?> GetBeatmapsetFromApi(
		int beatmapsetId
	) {
		var url = $"https://osu.ppy.sh/api/v2/beatmapsets/{beatmapsetId}";
		
		var response = await _bearerClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode && content.IsValidResponse())
		{
			var beatmapset = Deserialize<ApiBeatmapsetFull>(content);
			if (beatmapset == null) return null;
			
			await UpdateBeatmapset(beatmapset);
			
			return beatmapset;
		}
		
		return null;
	}
	
	private static T? Deserialize<T>(string content) => JsonSerializer.Deserialize<T?>(content, SnakeCaseNamingPolicy.Options);

	private async Task UpdateBeatmapset(
		ApiBeatmapsetFull beatmapset
	) {
		var cache = new Beatmapset(beatmapset);
		
		await beatmaps.InsertBeatmapset(cache);
		beatmapCache.InsertBeatmapset(cache);
		
		beatmapset.FavouriteCount = cache.FavoriteCount;
		beatmapset.PlayCount = cache.PlayCount;
		beatmapset.Ratings = cache.Ratings;
		beatmapset.Rating = cache.Rating;
		
		foreach (var beatmap in beatmapset.Beatmaps)
		{
			var cachedBeatmap = cache.Beatmaps.First(b => b.Id == beatmap.Id);
			
			beatmap.Playcount = cachedBeatmap.Plays;
			beatmap.Passcount = cachedBeatmap.Passes;
			beatmap.Failtimes = new Failtime
			{
				Fail = cachedBeatmap.Fails,
				Exit = cachedBeatmap.Exits
			};
		}
	}
}