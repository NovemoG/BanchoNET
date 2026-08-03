using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class LazerScoresRepository(BanchoDbContext dbContext) : ScoresRepository(dbContext), ILazerScoresRepository
{
    public async Task<ApiScore> InsertScore(
        ApiScore score,
        string md5,
        int mapId
    ) {
        var dbScore = DbContext.Scores
            .Add(new ScoreDto
            {
                BeatmapMD5 = md5,
                MapId = mapId,
                Preserve = score.Preserve,
                Processed = score.Processed,
                Ranked = score.Ranked,
                HasReplay = score.HasReplay,
                PP = (float)score.Pp,
                Acc = (float)score.Accuracy,
                LegacyTotalScore = score.TotalScore,
                MaxCombo = score.MaxCombo,
                Mods = score.Mods.ToLegacyMods(),
                ModKeys = score.ModKeys,
                LazerMods = score.ModsToString(),
                Statistics = score.Statistics,
                Grade = score.Grade,
                Status = score.Status,
                Mode = (GameMode)score.RulesetId,
                TimeElapsed = score.TimeElapsed,
                StartTime = score.StartedAt,
                PlayTime = score.EndedAt,
                PlayerId = score.UserId,
                LegacyPerfect = score.LegacyPerfect,
            });
        await DbContext.SaveChangesAsync();

        score.Id = dbScore.Entity.Id;
        return score;
    }
    
    public async Task UpdateScoreStatus(
        ApiScore? score
    ) {
        if (score == null) return;

        await UpdateScoreStatus(score.Id, score.Status);
    }

    public async Task<ApiScore?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        Beatmap beatmap
    ) {
        var score = await DbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .FirstOrDefaultAsync(s =>
                s.MapId == beatmap.Id
                && s.PlayerId == playerId
                && s.Mode == mode
                && s.Status == SubmissionStatus.Best);

        return score == null ? null : new ApiScore(score, score.Player, beatmap);
    }

    public async Task<ApiScore?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        string mods,
        Beatmap beatmap
    ) {
        var score = await DbContext.Scores
            .AsNoTracking()
            .Include(scoreDto => scoreDto.Player)
            .FirstOrDefaultAsync(s =>
                s.MapId == beatmap.Id
                && s.PlayerId == playerId
                && s.Mode == mode
                && s.ModKeys == mods
                && s.Status >= SubmissionStatus.BestWithMods);
        
        return score == null ? null : new ApiScore(score, score.Player, beatmap);
    }

    public async Task SetScoreLeaderboardPosition(
        ApiScore score,
        bool withMods,
        Beatmap beatmap,
        string mods = ""
    ) {
        score.LeaderboardPosition = await DbContext.Scores
            .Include(s => s.Player)
            .Where(s =>
                s.MapId == beatmap.Id
                && (s.Player.Privileges & 1) == 1
                && s.Mode == (GameMode)score.RulesetId
                && (withMods
                    ? s.Status >= SubmissionStatus.BestWithMods
                    : s.Status == SubmissionStatus.Best)
                && (!withMods || s.ModKeys == mods)
                && (OrderByPp((GameMode)score.RulesetId)
                    ? s.PP > Math.Round(score.Pp, 3, MidpointRounding.AwayFromZero)
                    : score.TotalScore < s.LegacyTotalScore))
            .CountAsync() + 1;
    }

    public async Task<(List<ApiScore>, int, ApiScore?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        string mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        Beatmap beatmap
    ) {
        var mapId = beatmap.Id;
        
        var leaderboard = (await GetBeatmapLeaderboardInternal(
                mode,
                type,
                mods,
                country,
                friendIds,
                mapId,
                null
            )).Select(s => new ApiScore(s, s.Player, beatmap))
            .ToList();

        ApiScore? playerBest = null;
         if (leaderboard.Count > 0)
        {
            var withMods = type is LeaderboardType.Mods or LeaderboardType.CountryMods or LeaderboardType.FriendsMods or LeaderboardType.TeamMods;
            playerBest = withMods
                ? await GetPlayerBestScoreWithModsOnMap(playerId, mode, mods, beatmap)
                : await GetPlayerBestScoreOnMap(playerId, mode, beatmap);

            if (playerBest != null)
                await SetScoreLeaderboardPosition(playerBest, withMods, beatmap, mods);
        }

        return (leaderboard, leaderboard.Count, playerBest);
    }
}