using BanchoNET.Models;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	public async Task<Beatmap?> GetBeatmap(int mapId = -1, int setId = -1, string beatmapMD5 = "")
	{
		if (!string.IsNullOrEmpty(beatmapMD5))
			return await GetBeatmapWithMD5(beatmapMD5, setId);
		
		if (mapId > 0)
			return await GetBeatmapWithId(mapId);
		
		return null;
	}
	
	public async Task<BeatmapSet?> GetBeatmapSet(int setId)
	{
		return null;
	}
	
	public async Task<bool> EnsureLocalBeatmapFile(int beatmapID, string beatmapMD5)
	{
		var beatmapPath = Path.Combine(Storage.BeatmapsPath, $"{beatmapID}.osu");

		if (!File.Exists(beatmapPath) ||
		    !beatmapPath.CheckLocalBeatmapMD5(beatmapMD5))
		{
			var response = await _httpClient.GetAsync($"https://old.ppy.sh/osu/{beatmapID}");

			if (!response.IsSuccessStatusCode)
				return false;
			
			await File.WriteAllBytesAsync(beatmapPath, await response.Content.ReadAsByteArrayAsync());
		}
		
		return true;
	}

	private async Task<Beatmap?> GetBeatmapWithMD5(string beatmapMD5, int setId)
	{
		var beatmap = _session.GetBeatmap(beatmapMD5: beatmapMD5);

		if (beatmap == null)
		{
			if (setId <= 0)
			{
				var map = await _dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MD5 == beatmapMD5);

				if (map != null)
					setId = map.SetId;
				else
				{
					var apiMap = await GetBeatmapFromApi(beatmapMD5: beatmapMD5);
					if (apiMap == null) return null;

					setId = apiMap.SetId;
				}
			}

			var beatmapSet = await GetBeatmapSet(setId);

			if (beatmapSet != null)
				return beatmapSet.Beatmaps.First(b => b.MD5 == beatmapMD5);
		}

		if (beatmap != null)
		{
			//TODO cache as expired
		}
	}

	private async Task<Beatmap?> GetBeatmapWithId(int mapId)
	{
		var beatmap = _session.GetBeatmap(mapId: mapId);
	}
	
	private async Task<Beatmap?> GetBeatmapFromApi(string beatmapMD5 = "", int mapId = -1, int setId = -1)
	{
		var osuApiKeyProvided = !string.IsNullOrEmpty(_config.OsuApiKey);
		
		var url = osuApiKeyProvided ?
			$"https://old.ppy.sh/api/get_beatmaps?k={_config.OsuApiKey}" :
			"https://osu.direct/api/get_beatmaps";
		
		var paramsSign = osuApiKeyProvided ? "&" : "?";

		if (!string.IsNullOrEmpty(beatmapMD5))
			url += $"{paramsSign}h={beatmapMD5}";
		else if (mapId > -1)
			url += $"{paramsSign}b={beatmapMD5}";
		else if (setId > -1)
			url += $"{paramsSign}s={beatmapMD5}";

		var response = await _httpClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();
		
		if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(content))
			return new Beatmap(JsonConvert.DeserializeObject<ApiBeatmap>(content)!);

		return null;
	}
}