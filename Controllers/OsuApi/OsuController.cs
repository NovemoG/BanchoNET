using BanchoNET.Attributes;
using BanchoNET.Services;
using BanchoNET.Services.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[Route("web")]
[ApiController]
[SubdomainAuthorize(["osu"])]
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