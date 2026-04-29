using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Scores;

[Route("api/v2/scores")]
public partial class ScoresController(
    IAuthService auth,
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapsRepository beatmaps,
    ILazerScoresRepository scores
) : ApiController(auth, players, playerService, beatmaps)
{
    [HttpGet("recent")]
    public async Task<ActionResult<List<ApiScoreExtended>>> GetRecentScores(
        [FromQuery] int offset,
        [FromQuery] int limit,
        [FromQuery] string mode
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();
        
        var recentScores = await scores.GetRecentScores(gameMode, offset, limit);
        
        return JsonSnake(recentScores.Select(s => new ApiScoreExtended(s, s.Player, s.Beatmap)));
    }
}