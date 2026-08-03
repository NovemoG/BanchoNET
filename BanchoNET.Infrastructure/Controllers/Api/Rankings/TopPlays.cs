using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Rankings;

public partial class RankingsController
{
    [HttpGet("top-plays/{mode}")]
    [AllowClientCredentials]
    public async Task<ActionResult<List<ApiScoreExtended>>> GetTopScores(
        string mode,
        [FromQuery] int page
    ) {
        if (page < 1) return BadRequest();
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();
        
        var bestScores = await scores.GetBestScores(gameMode, (page - 1) * 50);
        var returnList = bestScores
            .Select(s => new ApiScoreExtended(s, s.Player, s.Beatmap))
            .ToList();

        return JsonSnake(returnList);
    }
    
    [HttpGet("top-plays/{mode}/count")]
    [AllowClientCredentials]
    public async Task<IActionResult> GetTopScoresCount(
        string mode
    ) {
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();

        return JsonSnake(new { total = await scores.GetBestScoresCount(gameMode) });
    }
}