using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpGet("{beatmapId:int}/scores")]
    public async Task<ActionResult<BeatmapScoresResponseDto>> GetScores(
        int beatmapId,
        [FromQuery] string type,
        [FromQuery] string mode,
        [FromQuery(Name = "mods[]")] string[] mods, 
        [FromQuery] int limit,
        [FromQuery] string sort = "score",
        [FromQuery(Name = "score_display")] string scoreDisplay = "score"
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        if (limit is < 0 or > 100
            || scoreDisplay is not ("score" or "pp")
            || !Enum.TryParse<LeaderboardSort>(sort, true, out var sorting)
            || !Enum.IsDefined(sorting))
        {
            return BadRequest();
        }

        if (sorting == LeaderboardSort.Score && scoreDisplay == "pp")
            sorting = LeaderboardSort.PP;

        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();

        var beatmap = await Beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null || beatmap.Status < BeatmapStatus.Ranked) return NotFound();

        var withMods = mods.Length > 0;
        var leaderboardType = type switch
        {
            "global" when !withMods => LeaderboardType.Top,
            "global" when withMods => LeaderboardType.Mods,
            "country" when !withMods => LeaderboardType.Country,
            "country" when withMods => LeaderboardType.CountryMods,
            "friend" when !withMods => LeaderboardType.Friends,
            "friend" when withMods => LeaderboardType.FriendsMods,
            "team" when !withMods => LeaderboardType.Team,
            "team" when withMods => LeaderboardType.TeamMods,
            _ => LeaderboardType.Local
        };

        if (leaderboardType == LeaderboardType.Local)
            return BadRequest();

        Player? player = null;

        if (leaderboardType >= LeaderboardType.Country)
        {
            player = await Players.GetPlayerOrOffline(uid);
            if (player == null) return NotFound();
        }

        var modsString = string.Empty;
        if (withMods && mods[0] != "NM")
            modsString = string.Join(';', mods.Order()).ToUpper() + ";";

        var (leaderboard, count, playerBest, position) = await scores.GetLeaderboardScores(
            beatmap.Id,
            gameMode,
            leaderboardType,
            modsString,
            uid,
            player?.CountryCode.ToString().ToLower() ?? "",
            player?.Friends.ToHashSet() ?? [],
            [],
            scoreDisplay == "pp",
            sorting,
            limit
        );

        var response = new BeatmapScoresResponseDto
        {
            ScoreCount = count,
            UserScore = playerBest == null ? null : new UserScore
            {
                Position = position,
                Score = new ApiScore(playerBest, playerBest.Player, beatmap)
            },
            Scores = leaderboard.Select(s => new ApiScore(s, s.Player, beatmap)).ToList()
        };

        return JsonSnake(response);
    }
}