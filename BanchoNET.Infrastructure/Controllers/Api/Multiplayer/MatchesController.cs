using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Multiplayer;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Multiplayer;

[Route("api/v2/matches")]
public class MatchesController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IMultiplayerHistoryRepository matchHistory
) : ApiControllerBase(players, playerService, beatmaps)
{
    private const int MaxLimit = 50;
    
    [HttpGet]
    [AllowClientCredentials]
    public async Task<ActionResult<MatchListResponse>> GetMatches(
        [FromQuery] long? before = null,
        [FromQuery] int limit = 20
    ) {
        limit = Math.Clamp(limit, 1, MaxLimit);

        var matches = await matchHistory.GetMatches(before, limit);

        return JsonSnake(new MatchListResponse
        {
            Matches = matches.Select(m => m.ToSummary()).ToList(),
            NextCursor = matches.Count == limit ? matches[^1].Id : null
        });
    }

    [HttpGet("{matchId:long}")]
    [AllowClientCredentials]
    public async Task<ActionResult<MatchResponse>> GetMatch(long matchId)
    {
        var match = await matchHistory.GetMatch(matchId);
        if (match == null) return NotFound();

        return JsonSnake(match.ToResponse());
    }
}