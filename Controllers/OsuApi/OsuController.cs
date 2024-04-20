using BanchoNET.Services;
using BanchoNET.Services.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[Route("web")]
[ApiController]
public partial class OsuController(
    PlayersRepository players,
    BeatmapsRepository beatmaps,
    ScoresRepository scores,
    BeatmapHandler beatmapHandler,
    GeolocService geoloc,
    HttpClient httpClient)
    : ControllerBase
{
    private readonly BanchoSession _session = BanchoSession.Instance;

    [HttpPost("osu-error.php")]
    public async Task<IActionResult> OsuError()
    {
        Console.WriteLine("OsuError");
        return Ok("");
    }

    [HttpGet("bancho_connect.php")]
    public async Task<IActionResult> BanchoConnect()
    {
        Console.WriteLine("BanchoConnect");
        return Ok();
    }

    [HttpGet("check-updates.php")]
    public async Task<IActionResult> CheckUpdates()
    {
        Console.WriteLine("CheckUpdates");
        return Ok();
    }
}