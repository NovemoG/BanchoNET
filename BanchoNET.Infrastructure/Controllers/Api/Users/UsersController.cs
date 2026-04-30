using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

//TODO route can also look like this: api/v2/users/username/?key=username
[Route("api/v2/users/{userId:int}")]
public partial class UsersController(
    IAuthService auth,
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapsRepository beatmaps,
    ILazerScoresRepository scores
) : ApiController(auth, players, playerService, beatmaps)
{
    [HttpGet("{forMode?}")]
    public async Task<ActionResult<ApiPlayer?>> GetUsers(
        int userId,
        string? forMode = null,
        [FromQuery] string key = "id"
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        var mode = GameMode.RelaxStd;
        if (!string.IsNullOrWhiteSpace(forMode))
            if (!EnumExtensions.ToModeMap.TryGetValue(forMode, out mode))
                return BadRequest();

        var apiPlayer = await Players.GetPlayerInfoForMode<ApiPlayer>(userId, mode);
        if (apiPlayer == null) return NotFound();
        
        mode = EnumExtensions.ToModeMap[apiPlayer.Playmode];
        
        apiPlayer.ScoresFirstCount = await scores.PlayerFirstPlaceScoresCount(userId, mode);
        apiPlayer.ScoresRecentCount = await scores.PlayerRecentScoresCount(userId, mode);

        return JsonSnake(apiPlayer);
    }
}