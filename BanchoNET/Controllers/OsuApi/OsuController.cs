using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[Route("web")]
[ApiController]
[SubdomainAuthorize("osu")]
public partial class OsuController(
    IPlayerService playerService,
    IBeatmapService beatmapService,
    IPlayersRepository players,
    IChannelService channels,
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