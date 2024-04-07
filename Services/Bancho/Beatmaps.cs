using System.Web;
using BanchoNET.Models.Beatmaps;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	
	public async Task<bool> CheckIfMapExistsOnBanchoByFilename(string filename)
	{
		var response = await _httpClient.GetAsync($"https://osu.ppy.sh/web/maps/{filename}");
		return response.Content.Headers.ContentLength > 0;
	}
	
	public async Task<bool> EnsureLocalBeatmapFile(int beatmapId, string beatmapMD5)
	{
		//TODO fix saving beatmap file
		var beatmapPath = Storage.GetBeatmapPath(beatmapId);

		if (!File.Exists(beatmapPath) ||
		    !beatmapPath.CheckLocalBeatmapMD5(beatmapMD5))
		{
			var response = await _httpClient.GetAsync($"https://old.ppy.sh/osu/{beatmapId}");
			if (response.Content.Headers.ContentLength == 0)
				return false;

			await using var fileStream = new FileStream(beatmapPath, FileMode.Create, FileAccess.ReadWrite);
			await response.Content.CopyToAsync(fileStream);

			// await response.Content.CopyToAsync(new FileStream(beatmapPath, FileMode.Create));
		}
		
		return true;
	}

	public async Task UpdateBeatmapStats(Beatmap beatmap)
	{
		await _dbContext.Beatmaps.Where(b => b.MapId == beatmap.MapId)
		                .ExecuteUpdateAsync(p => 
			                p.SetProperty(b => b.Plays, beatmap.Plays)
			                 .SetProperty(b => b.Passes, beatmap.Passes));
	}
	
	public async Task<Beatmap?> GetBeatmap(int mapId = -1, int setId = -1, string beatmapMD5 = "")
	{
		if (!string.IsNullOrEmpty(beatmapMD5))
		{
			var map = await GetBeatmapWithMD5(beatmapMD5, setId);
			if (map != null) return map;
			return null;


			// return await GetBeatmapWithMD5(beatmapMD5, setId);
		}

		if (mapId > 0)
		{
			var map = await GetBeatmapWithId(mapId);
			if (map != null) return map;
			return null;
		}

		return null;
	}

	private async Task<Beatmap?> GetBeatmapWithMD5(string beatmapMD5, int setId)
	{
		var beatmap = _session.GetBeatmap(beatmapMD5: beatmapMD5);
		if (beatmap != null) return beatmap;
		
		var mapId = 0;
			
		if (setId <= 0)
		{
			var map = await _dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MD5 == beatmapMD5);

			if (map != null)
			{
				setId = map.SetId;
				mapId = map.MapId;
			}
			else
			{
				var apiMap = await GetBeatmapFromApi(beatmapMD5: beatmapMD5);
				if (apiMap == null) return null;

				setId = apiMap.SetId;
				mapId = apiMap.MapId;
			}
		}

		var beatmapSet = await GetBeatmapSet(setId, mapId);

		//TODO: When user needs update
		if (beatmapSet != null) return beatmapSet.Beatmaps.FirstOrDefault(b => b.MD5 == beatmapMD5);

		return beatmap;
	}

	private async Task<Beatmap?> GetBeatmapWithId(int mapId)
	{
		var beatmap = _session.GetBeatmap(mapId: mapId);
		if (beatmap != null) return beatmap;
		
		int setId;
		var map = await _dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MapId == mapId);
			
		if (map != null)
			setId = map.SetId;
		else
		{
			var apiMap = await GetBeatmapFromApi(mapId: mapId);
			if (apiMap == null) return null;

			setId = apiMap.SetId;
		}

		var beatmapSet = await GetBeatmapSet(setId, mapId);

		if (beatmapSet != null)
			return beatmapSet.Beatmaps.FirstOrDefault(b => b.MapId == mapId);

		return beatmap;
	}

	private async Task<BeatmapSet?> GetBeatmapSet(int setId, int mapId = 0)
	{
		bool didApiRequest = false;
		var beatmapSet = _session.GetBeatmapSet(setId);
		
		if (beatmapSet == null)
		{
			var dbBeatmaps = await _dbContext.Beatmaps
			                                 .Where(b => b.SetId == setId)
			                                 .ToListAsync();

			if (dbBeatmaps.Count == 0)
			{
				beatmapSet = await GetBeatmapSetFromApi(setId);
				if (beatmapSet == null) return null;

				didApiRequest = true;
				_session.CacheBeatmapSet(beatmapSet);
				await InsertSetIntoDatabase(beatmapSet);
			}
			else
				beatmapSet = new BeatmapSet(dbBeatmaps);
		}

		if (!didApiRequest && mapId > 0)
			if (beatmapSet.Beatmaps.All(b => b.MapId != mapId))
				await UpdateBeatmapSet(setId);
		
		return beatmapSet;
	}

	private async Task UpdateBeatmapSet(int setId)
	{
		var beatmapSet = await GetBeatmapSetFromApi(setId);
		if (beatmapSet == null) return;
		
		_session.CacheBeatmapSet(beatmapSet);
		await InsertSetIntoDatabase(beatmapSet);
	}
	
	private async Task<Beatmap?> GetBeatmapFromApi(string beatmapMD5 = "", int mapId = -1)
	{
		var osuApiKeyProvided = !string.IsNullOrEmpty(AppSettings.OsuApiKey);
		
		var url = osuApiKeyProvided ?
			$"https://osu.ppy.sh/api/get_beatmaps?k={AppSettings.OsuApiKey}" :
			"https://osu.direct/api/get_beatmaps";
		
		var paramsSign = osuApiKeyProvided ? "&" : "?";

		if (!string.IsNullOrEmpty(beatmapMD5))
			url += $"{paramsSign}h={beatmapMD5}";
		else if (mapId > -1)
			url += $"{paramsSign}b={mapId}";
		else
			return null;

		var response = await _httpClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();
		
		if (response.IsSuccessStatusCode && content.IsValidResponse())
			return osuApiKeyProvided
				? new Beatmap(JsonConvert.DeserializeObject<List<OsuApiBeatmap>>(content)![0])
				: new Beatmap(JsonConvert.DeserializeObject<List<ApiBeatmap>>(content)![0]);

		return null;
	}

	private async Task<BeatmapSet?> GetBeatmapSetFromApi(int setId)
	{
		if (setId <= 0) return null;
		
		var osuApiKeyProvided = !string.IsNullOrEmpty(AppSettings.OsuApiKey);
		
		var url = osuApiKeyProvided ?
			$"https://osu.ppy.sh/api/get_beatmaps?k={AppSettings.OsuApiKey}&s={setId}" :
			$"https://osu.direct/api/get_beatmaps?s={setId}";
		
		var response = await _httpClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode && content.IsValidResponse())
			return osuApiKeyProvided
				? new BeatmapSet(JsonConvert.DeserializeObject<List<OsuApiBeatmap>>(content)!)
				: new BeatmapSet(JsonConvert.DeserializeObject<List<ApiBeatmap>>(content)!);

		return null;
	}

	private async Task InsertSetIntoDatabase(BeatmapSet set)
	{
		foreach (var beatmap in set.Beatmaps)
		{
			var dbBeatmap = await _dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MapId == beatmap.MapId);

			if (dbBeatmap != null)
			{
				dbBeatmap = UpdateBeatmapDto(beatmap, dbBeatmap);
				_dbContext.Update(dbBeatmap);
			}
			else
			{
				await _dbContext.Beatmaps.AddAsync(CreateBeatmapDto(beatmap));
			}
		}
		
		await _dbContext.SaveChangesAsync();
	}

	private BeatmapDto CreateBeatmapDto(Beatmap beatmap)
	{
		return new BeatmapDto
		{
			MapId = beatmap.MapId,
			SetId = beatmap.SetId,
			Private = beatmap.Private,
			Mode = (byte)beatmap.Mode,
			Status = (sbyte)beatmap.Status,
			IsRankedOfficially = beatmap.IsRankedOfficially,
			MD5 = beatmap.MD5,
			Artist = beatmap.Artist,
			Title = beatmap.Title,
			Name = beatmap.Name,
			Creator = beatmap.Creator,
			SubmitDate = beatmap.SubmitDate,
			LastUpdate = beatmap.LastUpdate,
			TotalLength = beatmap.TotalLength,
			MaxCombo = beatmap.MaxCombo,
			Plays = beatmap.Plays,
			Passes = beatmap.Passes,
			Bpm = beatmap.Bpm,
			Cs = beatmap.Cs,
			Ar = beatmap.Ar,
			Od = beatmap.Od,
			Hp = beatmap.Hp,
			StarRating = beatmap.StarRating,
			NotesCount = beatmap.NotesCount,
			SlidersCount = beatmap.SlidersCount,
			SpinnersCount = beatmap.SpinnersCount
		};
	}

	private BeatmapDto UpdateBeatmapDto(Beatmap beatmap, BeatmapDto currentBeatmap)
	{
		return new BeatmapDto
		{
			MapId = currentBeatmap.MapId,
			SetId = currentBeatmap.SetId,
			Private = currentBeatmap.Private,
			Mode = currentBeatmap.Mode,
			Status = currentBeatmap.Status,
			IsRankedOfficially = beatmap.IsRankedOfficially,
			MD5 = beatmap.MD5,
			Artist = beatmap.Artist,
			Title = beatmap.Title,
			Name = beatmap.Name,
			Creator = beatmap.Creator,
			SubmitDate = beatmap.SubmitDate,
			LastUpdate = beatmap.LastUpdate,
			TotalLength = beatmap.TotalLength,
			MaxCombo = beatmap.MaxCombo,
			Plays = currentBeatmap.Plays,
			Passes = currentBeatmap.Passes,
			Bpm = beatmap.Bpm,
			Cs = beatmap.Cs,
			Ar = beatmap.Ar,
			Od = beatmap.Od,
			Hp = beatmap.Hp,
			StarRating = currentBeatmap.StarRating,
			NotesCount = currentBeatmap.NotesCount,
			SlidersCount = currentBeatmap.SlidersCount,
			SpinnersCount = currentBeatmap.SpinnersCount
		};
	}
}