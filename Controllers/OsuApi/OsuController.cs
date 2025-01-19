using BanchoNET.Abstractions.Repositories;
using BanchoNET.Abstractions.Services;
using BanchoNET.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[Route("web")]
[ApiController]
[SubdomainAuthorize("osu")]
public partial class OsuController(
    IBanchoSession session,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps,
    IScoresRepository scores,
    IBeatmapHandler beatmapHandler,
    IGeolocService geoloc,
    ILogger logger,
    HttpClient httpClient)
    : ControllerBase
{
    [HttpGet("bancho_connect.php")]
    public async Task<IActionResult> BanchoConnect()
    {
        return Ok();
    }

    [HttpGet("check-updates.php")]
    public async Task<IActionResult> CheckUpdates()
    {
        return Ok();
    }
}