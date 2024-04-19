using BanchoNET.Models;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
    [HttpPost("osu-getbeatmapinfo.php")]
    public async Task<IActionResult> OsuGetBeatmapInfo(
        [FromForm] OsuBeatmapInfoForm formData,
        [FromQuery(Name = "u")] string username,
        [FromQuery(Name = "h")] string passwordMD5)
    {
        var player = await _bancho.GetPlayerFromLogin(username, passwordMD5);
        if (player == null)
            return Unauthorized("auth fail");
        
        Console.WriteLine(formData.Filenames.Count);
        //TODO

        return Responses.BytesContentResult("");
    }
}