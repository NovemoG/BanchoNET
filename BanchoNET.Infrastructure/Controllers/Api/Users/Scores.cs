using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("scores/best")]
    public async Task<ActionResult<List<ApiScoreBest>>> GetBestScores(
        int userId,
        [FromQuery] int offset,
        [FromQuery] int limit,
        [FromQuery] string mode
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();

        var player = await Players.GetPlayerInfo(userId);
        if (player == null) return NotFound();
        
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();
        
        var bestScores = await scores.GetPlayerBestScores(userId, gameMode, offset, limit);
        
        var bestScoresList = new List<ApiScoreBest>();
        foreach (var score in bestScores)
        {
            var beatmap = await Beatmaps.GetBeatmap(score.MapId);
            if (beatmap == null)
            {
                offset++;
                continue;
            }
            
            bestScoresList.Add(new ApiScoreBest(score, player, beatmap, beatmap.Set, offset));
            offset++;
        }

        return JsonSnake(bestScoresList);
    }
    
    [HttpGet("scores/{type}")]
    public async Task<ActionResult<List<ApiScoreExtended>>> GetScores(
        int userId,
        string type,
        [FromQuery] int offset,
        [FromQuery] int limit,
        [FromQuery] string mode
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode)) return BadRequest();

        return JsonSnake(new List<ApiScoreExtended>());
    }
}