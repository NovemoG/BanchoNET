using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Multiplayer;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    private const int MaxMatchesLimit = 50;

    [HttpGet("{userId:int}/matches")]
    [AllowClientCredentials]
    public async Task<ActionResult<PlayerMatchListResponse>> GetMatches(
        int userId,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 20
    ) {
        var player = await Players.GetPlayerInfo(userId);
        if (player == null) return NotFound();

        limit = Math.Clamp(limit, 1, MaxMatchesLimit);

        var matches = await matchHistory.GetPlayerMatches(userId, Math.Max(0, offset), limit);

        return JsonSnake(new PlayerMatchListResponse
        {
            Matches = matches.Select(m => m.ToSummary()).ToList(),
            Total = await matchHistory.GetPlayerMatchesCount(userId)
        });
    }
}