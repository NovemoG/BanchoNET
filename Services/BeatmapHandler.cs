using BanchoNET.Abstractions.Services;
using BanchoNET.Models.Beatmaps;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using Newtonsoft.Json;

namespace BanchoNET.Services;

public class BeatmapHandler(HttpClient httpClient) : IBeatmapHandler
{
    public async Task<bool> CheckIfMapExistsOnBanchoByFilename(string filename)
	{
		var response = await httpClient.GetAsync($"https://osu.ppy.sh/web/maps/{filename}");
		return response.Content.Headers.ContentLength > 0;
	}
	
	public async Task<bool> EnsureLocalBeatmapFile(int beatmapId, string beatmapMD5)
	{
		var beatmapPath = Storage.GetBeatmapPath(beatmapId);

		if (!File.Exists(beatmapPath) ||
		    !beatmapPath.CheckLocalBeatmapMD5(beatmapMD5))
		{
			var response = await httpClient.GetAsync($"https://old.ppy.sh/osu/{beatmapId}");
			if (response.Content.Headers.ContentLength == 0)
				return false;

			await using var fileStream = new FileStream(beatmapPath, FileMode.Create, FileAccess.ReadWrite);
			await response.Content.CopyToAsync(fileStream);
		}
		
		return true;
	}
	
	public async Task<Beatmap?> GetBeatmapFromApi(string beatmapMD5 = "", int mapId = -1)
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

		var response = await httpClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();
		
		if (response.IsSuccessStatusCode && content.IsValidResponse())
			return osuApiKeyProvided
				? new Beatmap(JsonConvert.DeserializeObject<List<OsuApiBeatmap>>(content)![0])
				: new Beatmap(JsonConvert.DeserializeObject<List<ApiBeatmap>>(content)![0]);

		return null;
	}

	public async Task<BeatmapSet?> GetBeatmapSetFromApi(int setId)
	{
		if (setId <= 0) return null;
		
		var osuApiKeyProvided = !string.IsNullOrEmpty(AppSettings.OsuApiKey);
		
		var url = osuApiKeyProvided ?
			$"https://osu.ppy.sh/api/get_beatmaps?k={AppSettings.OsuApiKey}&s={setId}" :
			$"https://osu.direct/api/get_beatmaps?s={setId}";
		
		var response = await httpClient.GetAsync(url);
		var content = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode && content.IsValidResponse())
			return osuApiKeyProvided
				? new BeatmapSet(JsonConvert.DeserializeObject<List<OsuApiBeatmap>>(content)!)
				: new BeatmapSet(JsonConvert.DeserializeObject<List<ApiBeatmap>>(content)!);

		return null;
	}
}