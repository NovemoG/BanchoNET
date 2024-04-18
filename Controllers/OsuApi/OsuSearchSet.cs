using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
    [HttpGet("/web/osu-search-set.php")]
    public async Task<IActionResult> OsuSearchSet(
        [FromQuery(Name = "u")] string username,
        [FromQuery(Name = "h")] string passwordMD5,
        [FromQuery(Name = "s")] int rankedStatus,
        [FromQuery(Name = "b")] int mode)
    {
        if (await _bancho.GetPlayerFromLogin(username, passwordMD5) == null)
            return Unauthorized("auth fail");

        Console.WriteLine("[OsuSearchSet] Request");
        //TODO
        
        return Ok();
    }
}