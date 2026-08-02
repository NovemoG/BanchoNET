using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("{userId:int}/scores/best")]
    [AllowClientCredentials]
    public async Task<ActionResult<List<ApiScoreBest>>> GetBestScores(
        int userId,
        [FromQuery] int offset,
        [FromQuery] int limit,
        [FromQuery] string mode
    ) {
        var player = await Players.GetPlayerInfo(userId);
        if (player == null) return NotFound();
        
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();
        
        var bestScores = await scores.GetPlayerBestScores(userId, gameMode, offset, limit);

        return JsonSnake(bestScores.Select((s, i) => new ApiScoreBest(s, player, s.Beatmap, s.Beatmap.Beatmapset, i)));
    }
    
    [HttpGet("{userId:int}/scores/{type}")]
    [AllowClientCredentials]
    public async Task<ActionResult<List<ApiScoreExtended>>> GetScores(
        int userId,
        string type,
        [FromQuery] int offset,
        [FromQuery] int limit,
        [FromQuery] string mode
    ) {
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode)) return BadRequest();

        List<ScoreDto> tempScores;
        switch (type)
        {
            case "recent":
                tempScores = await scores.GetPlayerRecentScores(userId, gameMode, offset, limit);
                return JsonSnake(tempScores.Select(s => new ApiScoreExtended(s, s.Player, s.Beatmap, s.Beatmap.Beatmapset)));
            
            case "firsts":
                tempScores = await scores.GetPlayerFirstPlaceScores(userId, gameMode, offset, limit);
                return JsonSnake(tempScores.Select(s => new ApiScoreExtended(s, s.Player, s.Beatmap, s.Beatmap.Beatmapset)));
            
            case "pinned":
                return JsonSnake(new List<ApiScoreExtended>());
            
            default:
                return BadRequest();
        }
    }
}