using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
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
	// Beatmaps the official server has no file for, so they're never requested again
	private static readonly ConcurrentDictionary<int, byte> MissingFiles = new();

	private readonly HttpClient _bearerClient = httpClientFactory.CreateClient(AppSettings.TokenClientName);
	
	public async Task<bool> CheckIfMapExistsOnBanchoByFilename(
		string filename
	) {
		var response = await httpClient.GetAsync($"https://osu.ppy.sh/web/maps/{filename}");
		return response is { StatusCode: HttpStatusCode.OK, Content.Headers.ContentLength: > 0 };
	}

	public Task<BeatmapFileResult> EnsureLocalBeatmapFile(
		Beatmap beatmap
	) {
		return EnsureLocalBeatmapFile(beatmap.Id, beatmap.BeatmapsetId, beatmap.Checksum);
	}

	public async Task<BeatmapFileResult> EnsureLocalBeatmapFile(
		int mapId,
		int setId,
		string checksum
	) {
		var beatmapPath = Storage.GetBeatmapPath(mapId);

		if (File.Exists(beatmapPath) && beatmapPath.CheckLocalBeatmapMD5(checksum))
			return new BeatmapFileResult(BeatmapFileStatus.Current, checksum);

		var unavailable = new BeatmapFileResult(BeatmapFileStatus.Unavailable, checksum);
		if (MissingFiles.ContainsKey(mapId))
			return unavailable;

		var (file, missing) = await DownloadBeatmapFile(mapId);
		if (missing)
			MissingFiles.TryAdd(mapId, 0);

		if (file == null)
			return unavailable;

		var downloadedChecksum = Convert.ToHexStringLower(MD5.HashData(file));
		if (downloadedChecksum.Equals(checksum, StringComparison.OrdinalIgnoreCase))
		{
			logger.LogInfo($"Caching {mapId}.osu beatmap file");
			await File.WriteAllBytesAsync(beatmapPath, file);

			return new BeatmapFileResult(BeatmapFileStatus.Downloaded, checksum);
		}
		
		logger.LogInfo($"Beatmap {mapId} has a newer version ({checksum} -> {downloadedChecksum}), refreshing set {setId}");

		var set = await GetBeatmapset(setId, mapId, recheckApi: true);
		var refreshed = set?.Beatmaps.FirstOrDefault(b => b.Id == mapId);
		
		if (refreshed == null)
		{
			logger.LogWarning($"Couldn't refresh set {setId} after beatmap {mapId} was updated");
			return unavailable;
		}

		await File.WriteAllBytesAsync(beatmapPath, file);

		return new BeatmapFileResult(BeatmapFileStatus.Updated, refreshed.Checksum);
	}
	
	private async Task<(byte[]? File, bool Missing)> DownloadBeatmapFile(
		int mapId
	) {
		try
		{
			using var response = await httpClient.GetAsync($"https://old.ppy.sh/osu/{mapId}");

			if (response.StatusCode == HttpStatusCode.NotFound)
				return Missing();

			response.EnsureSuccessStatusCode();
			
			var file = await response.Content.ReadAsByteArrayAsync();
			return file.Length == 0 ? Missing() : (file, false);
		}
		catch (Exception ex)
		{
			logger.LogWarning($"Failed to download beatmap file for {mapId}", ex);
			return (null, false);
		}

		(byte[]?, bool) Missing()
		{
			logger.LogWarning($"Beatmap file {mapId} doesn't exist on the official server, it won't be requested again");
			return (null, true);
		}
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
			? beatmapset.Beatmaps.FirstOrDefault(b => b.Checksum.Equals(beatmapMD5, StringComparison.OrdinalIgnoreCase))
			: beatmap;
	}

	public async Task<Beatmapset?> GetBeatmapset(
		int setId,
		int mapId = -1,
		bool recheckApi = false,
		CancellationToken cancellationToken = default
	) {
		var beatmapset = beatmapCache.GetBeatmapset(setId);

		if (recheckApi)
		{
			var updated = await GetBeatmapsetFromApi(setId, cancellationToken);
			return updated == null ? null : beatmapCache.GetBeatmapset(setId);
		}

		if (beatmapset == null)
		{
			var dbBeatmapset = await beatmaps.GetBeatmapset(setId);
			if (dbBeatmapset == null)
			{
				var apiBeatmapset = await GetBeatmapsetFromApi(setId, cancellationToken);
				if (apiBeatmapset == null) return null;

				return beatmapCache.GetBeatmapset(setId);
			}

			beatmapset = new Beatmapset(dbBeatmapset);
			beatmapCache.InsertBeatmapset(beatmapset);
		}
		
		if (mapId > 0 && beatmapset.Beatmaps.All(b => b.Id != mapId))
		{
			var updated = await GetBeatmapsetFromApi(setId, cancellationToken);
			if (updated != null)
				return beatmapCache.GetBeatmapset(setId);
		}
		
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
		int beatmapsetId,
		CancellationToken cancellationToken = default
	) {
		var url = $"https://osu.ppy.sh/api/v2/beatmapsets/{beatmapsetId}";
		
		using var response = await _bearerClient.GetAsync(url, cancellationToken);
		var content = await response.Content.ReadAsStringAsync(cancellationToken);

		if (response.IsSuccessStatusCode && content.IsValidResponse())
		{
			var beatmapset = Deserialize<ApiBeatmapsetFull>(content);
			if (beatmapset == null) return null;
			
			await UpdateBeatmapset(beatmapset);
			
			return beatmapset;
		}
		
		return null;
	}
	
	public async Task<BeatmapsetImportResult> ImportBeatmapsetFromApi(
		int beatmapsetId,
		CancellationToken cancellationToken = default
	) {
		var url = $"https://osu.ppy.sh/api/v2/beatmapsets/{beatmapsetId}";

		using var response = await _bearerClient.GetAsync(url, cancellationToken);

		switch (response.StatusCode)
		{
			case HttpStatusCode.NotFound:
			case HttpStatusCode.Forbidden:
				return BeatmapsetImportResult.Unavailable;

			case HttpStatusCode.TooManyRequests:
				return BeatmapsetImportResult.RateLimited;
		}

		if (!response.IsSuccessStatusCode)
			return BeatmapsetImportResult.Failed;

		var content = await response.Content.ReadAsStringAsync(cancellationToken);
		if (!content.IsValidResponse())
			return BeatmapsetImportResult.Invalid;

		var beatmapset = Deserialize<ApiBeatmapsetFull>(content);
		if (beatmapset == null)
			return BeatmapsetImportResult.Invalid;

		var set = new Beatmapset(beatmapset);
		await beatmaps.InsertBeatmapset(set);

		// only refresh sets that are already cached, caching every imported set would hold the whole catalogue in memory
		if (beatmapCache.GetBeatmapset(beatmapsetId) != null)
			beatmapCache.InsertBeatmapset(set);

		return BeatmapsetImportResult.Imported;
	}

	public async Task<int?> GetNewestBeatmapsetIdFromApi(
		CancellationToken cancellationToken = default
	) {
		// new uploads count as updates, so the most recently updated sets probably include the newest one
		const string url = "https://osu.ppy.sh/api/v2/beatmapsets/search?s=any&sort=updated_desc&nsfw=true";

		using var response = await _bearerClient.GetAsync(url, cancellationToken);
		if (!response.IsSuccessStatusCode)
			return null;

		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

		if (!json.RootElement.TryGetProperty("beatmapsets", out var beatmapsets)
		    || beatmapsets.ValueKind != JsonValueKind.Array)
			return null;

		int? newest = null;
		foreach (var beatmapset in beatmapsets.EnumerateArray())
		{
			if (beatmapset.TryGetProperty("id", out var id)
			    && id.TryGetInt32(out var value)
			    && value > (newest ?? 0))
				newest = value;
		}

		return newest;
	}

	public async Task<bool> RestoreOfficialStatus(
		int setId,
		int mapId = -1,
		CancellationToken cancellationToken = default
	) {
		var stored = await beatmaps.GetBeatmapset(setId);
		if (stored == null) return false;
		
		ApiBeatmapsetFull? official = null;
		
		if (!stored.IsPrivateUpload)
		{
			using var response = await _bearerClient.GetAsync($"https://osu.ppy.sh/api/v2/beatmapsets/{setId}", cancellationToken);
			var content = await response.Content.ReadAsStringAsync(cancellationToken);

			if (response.IsSuccessStatusCode && content.IsValidResponse())
				official = Deserialize<ApiBeatmapsetFull>(content);
		}
		
		// sets the official server doesn't have go back to pending
		if (official == null)
			return mapId > 0
				? await beatmaps.UpdateBeatmapStatus(mapId, BeatmapStatus.LatestPending) > 0
				: await beatmaps.UpdateBeatmapsetStatus(setId, BeatmapStatus.LatestPending) > 0;
		
		var officialStatuses = official.Beatmaps.ToDictionary(b => b.Id, b => (BeatmapStatus)b.Ranked);
		
		await UpdateBeatmapset(official);
		
		return await beatmaps.RestoreOfficialStatuses(setId, officialStatuses, mapId) > 0;
	}
	
	private static T? Deserialize<T>(string content) => JsonSerializer.Deserialize<T?>(content, SnakeCaseNamingPolicy.Options);

	private async Task UpdateBeatmapset(
		ApiBeatmapsetFull beatmapset
	) {
		var cache = new Beatmapset(beatmapset);
		
		await beatmaps.InsertBeatmapset(cache);
		beatmapCache.InsertBeatmapset(cache);
		
		beatmapset.Status = cache.Status.ToApiBeatmapStatus();
		beatmapset.Ranked = (int)cache.Status;
		beatmapset.RankedDate = cache.RankedDate is { } rankedDate
			? new DateTimeOffset(DateTime.SpecifyKind(rankedDate, DateTimeKind.Utc))
			: null;
		beatmapset.FavouriteCount = cache.FavoriteCount;
		beatmapset.PlayCount = cache.PlayCount;
		beatmapset.Ratings = cache.Ratings.ToApiRatings();
		beatmapset.Rating = cache.Rating;
		beatmapset.IsRankedOfficially = cache.IsRankedOfficially;
		
		foreach (var beatmap in beatmapset.Beatmaps)
		{
			var cachedBeatmap = cache.Beatmaps.First(b => b.Id == beatmap.Id);
			
			beatmap.Status = cachedBeatmap.Status.ToApiBeatmapStatus();
			beatmap.Ranked = (int)cachedBeatmap.Status;
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