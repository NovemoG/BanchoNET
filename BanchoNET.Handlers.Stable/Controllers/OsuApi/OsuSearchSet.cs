using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Handlers.Stable.Controllers.OsuApi;

public partial class OsuController
{
    [HttpGet("osu-search-set.php")]
    public async Task<IActionResult> OsuSearchSet(
        [FromQuery(Name = "u")] string username,
        [FromQuery(Name = "h")] string passwordMD5,
        [FromQuery(Name = "s")] int mapSetId,
        [FromQuery(Name = "b")] int mapId)
    {
        if (await players.GetPlayerFromLogin(username, passwordMD5) == null)
            return Unauthorized("auth fail");

        var beatmap = await beatmapHandler.GetBeatmap(mapId, mapSetId);
        if (beatmap == null)
            return Ok();

        var set = beatmap.Set;
        
        //TODO replace 10.0 with actual rating
        var response = $"{beatmap.BeatmapsetId}.osz|{set.Artist}|{set.Title}|{set.CreatorName}|{beatmap.Status}|10.0|{beatmap.LastUpdated}|{beatmap.BeatmapsetId}|0|{set.Video}|0|0|0";
        return Responses.BytesContentResult(response);
    }
}