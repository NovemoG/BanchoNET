using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Handlers.Stable.Controllers.OsuApi;

[Route("web")]
[ApiController]
[SubdomainAuthorize("osu")]
public partial class OsuController(
    IPlayerService playerService,
    IBeatmapService beatmapService,
    IPlayersRepository players,
    IChannelService channels,
    IBeatmapsRepository beatmapsRepository,
    IBeatmapDownloader beatmapDownloader,
    IBeatmapSearchService beatmapSearch,
    ILegacyScoresRepository scores,
    IBeatmapHandler beatmapHandler,
    IGeolocService geoloc,
    IPasswordService passwords,
    ILogger logger
) : ControllerBase
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