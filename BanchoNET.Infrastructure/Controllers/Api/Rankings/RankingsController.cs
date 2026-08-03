using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Rankings;

[Route("api/v2/rankings")]
public partial class RankingsController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    ILazerScoresRepository scores
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpGet("{mode}/{type}")]
    [AllowClientCredentials]
    public async Task<ActionResult<RankingsResponse>> GetRankings(
        string mode,
        string type,
        [FromQuery] int page,
        [FromQuery] string? country,
        [FromQuery] int? spotlight,
        [FromQuery] string? filter
    ) {
        if (page < 1) return BadRequest();
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();
        
        int[]? friendIds = null;

        if (string.Equals(filter, "friends", StringComparison.OrdinalIgnoreCase)
            && User.TryGetUserId(out var uid))
        {
            var friends = await Players.GetPlayerFriends(uid);
            
            friendIds = friends.Select(f => f.TargetId).Append(uid).Distinct().ToArray();
        }

        switch (type)
        {
            case "performance":
                return await Ranking(gameMode, page, country?.ToLower() ?? "", friendIds: friendIds);

            case "score":
                return await Ranking(gameMode, page, country?.ToLower() ?? "", byScore: true, friendIds: friendIds);

            case "country":
                return await CountryRanking(gameMode, page);

            case "charts":
                return JsonSnake(new RankingsResponse());

            default:
                return BadRequest();
        }
    }
    
    [HttpGet("{mode}/countries")]
    [AllowClientCredentials]
    public async Task<IActionResult> GetRankingCountries(
        string mode
    ) {
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();

        var countries = await Players.GetRankedCountries((byte)gameMode);

        return JsonSnake(new
        {
            countries = countries
                .Select(code => code.ToUpper().ParseCountry())
                .OrderBy(country => country.Name, StringComparer.OrdinalIgnoreCase)
        });
    }

    private async Task<ActionResult> CountryRanking(
        GameMode mode,
        int page
    ) {
        var (ranking, total) = await Players.GetCountryRanking((byte)mode, page);

        return JsonSnake(new
        {
            cursor = new Cursor { Page = page },
            cursor_string = string.Empty,
            ranking = ranking.Select(c => new
            {
                active_users = c.ActiveUsers,
                code = c.Code.ToUpper(),
                country = c.Code.ToUpper().ParseCountry(),
                performance = c.Performance,
                play_count = c.PlayCount,
                ranked_score = c.RankedScore
            }),
            total
        });
    }

    private async Task<ActionResult> Ranking(
        GameMode mode,
        int page,
        string country,
        bool byScore = false,
        int[]? friendIds = null
    ) {
        var ranking = await Players.GetRanking((byte)mode, page, country, byScore, friendIds);
        var response = new RankingsResponse
        {
            Cursor = new Cursor
            {
                Page = page
            },
            Ranking = ranking.Select((r, i) => new Statistics(r, (page - 1) * 50 + i + 1)).ToList(),
            Total = friendIds?.Length ?? Math.Min(await Players.TotalPlayerCount(country: country), 10000)
        };

        return JsonSnake(response);
    }
}