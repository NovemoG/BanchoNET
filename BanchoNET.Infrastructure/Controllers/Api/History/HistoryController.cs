using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.History;

[Route("api/v2/history")]
public partial class HistoryController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IPlayerHistoryRepository playerHistories
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpGet("users/{userId:int}/{forMode?}")]
    [AllowClientCredentials]
    public async Task<ActionResult<PlayerHistoryResponse>> GetPlayerHistory(
        int userId,
        string? forMode = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] HistoryMetric[]? metrics = null
    ) {
        var userInfo = await Players.GetPlayerInfo(userId);
        if (userInfo == null) return NotFound();

        var playerMode = userInfo.PreferredMode;

        if (!string.IsNullOrWhiteSpace(forMode))
        {
            if (!EnumExtensions.ToModeMap.TryGetValue(forMode, out var parsedMode))
                return BadRequest();

            playerMode = parsedMode;
        }

        var wanted = metrics ?? [];
        var samples = new List<PlayerHistoryDto>();

        foreach (var granularity in Enum.GetValues<HistoryGranularity>())
        {
            samples.AddRange(await playerHistories.GetSeries(
                userId,
                (byte)playerMode,
                wanted,
                granularity,
                from
            ));
        }

        return JsonSnake(samples.ToResponse(userId, EnumExtensions.FromModeMap[playerMode.AsVanilla()]));
    }
}