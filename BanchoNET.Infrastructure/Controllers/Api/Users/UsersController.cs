using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

//TODO route can also look like this: api/v2/users/username/?key=username
[Route("api/v2/users")]
public partial class UsersController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IBeatmapsRepository beatmapsRepository,
    ILazerScoresRepository scores
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpGet("{userId:int}/{forMode?}")]
    [AllowClientCredentials]
    public async Task<ActionResult<ApiPlayer?>> GetUsers(
        int userId,
        string? forMode = null,
        [FromQuery] string key = "id"
    ) {
        GameMode? requestedMode = null;
        
        if (!string.IsNullOrWhiteSpace(forMode))
        {
            if (!EnumExtensions.ToModeMap.TryGetValue(forMode, out var parsedMode))
                return BadRequest();

            requestedMode = parsedMode;
        }

        var apiPlayer = await Players.GetPlayerInfoForMode<ApiPlayer>(userId, requestedMode);
        if (apiPlayer == null) return NotFound();

        var mode = requestedMode ?? EnumExtensions.ToModeMap[apiPlayer.Playmode];
        
        apiPlayer.ScoresFirstCount = await scores.PlayerFirstPlaceScoresCount(userId, mode);
        apiPlayer.ScoresRecentCount = await scores.PlayerRecentScoresCount(userId, mode);
        apiPlayer.IsOnline = await PlayerService.IsOnline(userId);
        
        return JsonSnake(apiPlayer);
    }
}