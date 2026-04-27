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

        if (type.Equals("country", StringComparison.OrdinalIgnoreCase))
            return JsonSnake(new RankingsResponse());

        var ranking = await Players.GetRanking((byte)gameMode, page, type.Equals("score", StringComparison.OrdinalIgnoreCase));
        
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