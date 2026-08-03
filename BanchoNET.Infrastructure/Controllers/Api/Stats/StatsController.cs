using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Stats;

[Route("api/v2/stats")]
public class StatsController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IServerStatsService stats
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpGet]
    [AllowClientCredentials]
    public async Task<IActionResult> GetStats()
    {
        return JsonSnake(new
        {
            games = stats.GetActiveGameCount(),
            online = await stats.GetOnlineCount(),
            registered = await Players.TotalPlayerCount()
        });
    }

    [HttpGet("history")]
    [AllowClientCredentials]
    public async Task<IActionResult> GetHistory()
    {
        var samples = await stats.GetOnlineHistory();

        return JsonSnake(new
        {
            online = samples.Select(sample => new
            {
                at = sample.At,
                count = sample.Count
            })
        });
    }
}