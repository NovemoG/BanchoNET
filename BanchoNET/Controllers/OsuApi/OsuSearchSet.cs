using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

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

        var beatmap = await beatmaps.GetBeatmap(mapId, mapSetId);

        if (beatmap == null)
            return Ok();
        
        //TODO replace 10.0 with actual rating
        var response = $"{beatmap.SetId}.osz|{beatmap.Artist}|{beatmap.Title}|{beatmap.Creator}|{beatmap.Status}|10.0|{beatmap.LastUpdate}|{beatmap.SetId}|0|{beatmap.HasVideo}|0|0|0";
        return Responses.BytesContentResult(response);
    }
}