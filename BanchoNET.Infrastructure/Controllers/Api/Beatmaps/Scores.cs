using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpGet("scores")]
    public async Task<ActionResult<BeatmapScoresResponseDto>> GetScores(
        int beatmapId,
        [FromQuery] string type,
        [FromQuery] string mode,
        [FromQuery] int limit
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        if (limit is < 0 or > 100) return BadRequest();
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();

        var beatmap = await Beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null) return NotFound();

        var leaderboardType = type switch
        {
            "global" => LeaderboardType.Top,
            "country" => LeaderboardType.Country,
            "friend" => LeaderboardType.Friends,
            _ => LeaderboardType.Top
        };
        
        var player = await Players.GetPlayerOrOffline(uid);
        if (player == null) return NotFound();
        
        var (leaderboardScores, playerBest) = await scores.GetLeaderboardScores(
            leaderboardType,
            gameMode,
            LegacyMods.None,
            uid,
            player.Geoloc.Country.Acronym,
            player.Friends.ToHashSet(),
            beatmapId
        );

        var response = new BeatmapScoresResponseDto
        {
            ScoreCount = leaderboardScores.Count, //TODO all scores on the beatmap
            UserScore = playerBest == null ? null : new UserScore
            {
                Position = playerBest.LeaderboardPosition,
                Score = new ApiScore(playerBest, player, beatmap)
            }
        };

        response.Scores.AddRange(
            leaderboardScores.Select(score => new ApiScore(score, score.Player, beatmap))
        );

        return JsonSnake(response);
    }
}