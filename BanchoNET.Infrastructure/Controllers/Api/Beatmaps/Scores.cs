using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Players;
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
        [FromQuery(Name = "mods[]")] string[] mods, 
        [FromQuery] int limit
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        if (limit is < 0 or > 100) return BadRequest();
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
            modsString = mods.Aggregate(string.Empty, (current, mod) => $"{current}{mod};").ToUpper();
        
        var (leaderboardScores, scoreCount, playerBest) = await scores.GetLeaderboardScores(
            leaderboardType,
            gameMode,
            modsString,
            uid,
            player?.CountryCode.ToString().ToLower() ?? "",
            player?.Friends.ToHashSet() ?? [],
            beatmap
        );

        var response = new BeatmapScoresResponseDto
        {
            ScoreCount = scoreCount,
            UserScore = playerBest == null ? null : new UserScore
            {
                Position = playerBest.LeaderboardPosition,
                Score = playerBest
            },
            Scores = leaderboardScores
        };

        return JsonSnake(response);
    }
}