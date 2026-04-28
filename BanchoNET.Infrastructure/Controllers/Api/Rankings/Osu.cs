using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Rankings;

public partial class RankingsController
{
    [HttpGet("{mode}/{type}")]
    public async Task<ActionResult<RankingsResponse>> GetRankings(
        string mode,
        string type,
        [FromQuery] int page,
        [FromQuery] int? spotlight,
        [FromQuery] int? filter
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        if (page < 1) return BadRequest();
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();

        switch (type)
        {
            case "performance":
                return await Ranking(gameMode, page);
            
            case "score":
                return await Ranking(gameMode, page, byScore: true);
            
            case "country":
                return JsonSnake(new RankingsResponse());
            
            case "charts":
                return JsonSnake(new RankingsResponse());
            
            default:
                return BadRequest();
        }
    }

    private async Task<ActionResult> Ranking(
        GameMode mode,
        int page,
        bool byScore = false
    ) {
        var ranking = await Players.GetRanking((byte)mode, page, byScore);
        var response = new RankingsResponse
        {
            Cursor = new Cursor
            {
                Page = page
            },
            Ranking = ranking.Select((r, i) => new Statistics(r, i + 1)).ToList(),
        };

        return JsonSnake(response);
    } 
}