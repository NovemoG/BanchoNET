using BanchoNET.Abstractions.Services;
using BanchoNET.Attributes;
using BanchoNET.Services;
using BanchoNET.Services.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[Route("web")]
[ApiController]
[SubdomainAuthorize("osu")]
public partial class OsuController(
    IBanchoSession session,
    PlayersRepository players,
    BeatmapsRepository beatmaps,
    ScoresRepository scores,
    IBeatmapHandler beatmapHandler,
    IGeolocService geoloc,
    HttpClient httpClient)
    : ControllerBase
{
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