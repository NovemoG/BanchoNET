using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api;
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
        bool isPlayerRestricted,
        string md5,
        int mapId
    ) {
        var stats = score.Statistics;
        
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
                Mods = (int)score.LegacyMods, //TODO temporary
                LazerMods = score.ModsToString(),
                Count300 = stats.Great,
                Count100 = stats.Ok,
                Count50 = stats.Meh,
                Misses = stats.Miss,
                Gekis = stats.LargeTickHit,
                Katus = stats.SliderTailHit,
                IgnoreHit = stats.IgnoreHit,
                IgnoreMiss = stats.IgnoreMiss,
                Grade = (byte)score.Grade,
                Status = (byte)score.Status,
                Mode = (byte)score.RulesetId,
                TimeElapsed = score.TimeElapsed,
                StartTime = score.StartedAt,
                PlayTime = score.EndedAt,
                PlayerId = score.UserId,
                LegacyPerfect = score.LegacyPerfect,
                IsRestricted = isPlayerRestricted
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
                && !s.IsRestricted
                && s.PlayerId == playerId
                && s.Mode == (int)mode
                && s.Status == (int)SubmissionStatus.Best);

        return score == null ? null : new ApiScore(score, score.Player, beatmap);
    }

    public async Task<ApiScore?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        List<ApiMod> mods,
        Beatmap beatmap
    ) {
        var score = await DbContext.Scores
            .AsNoTracking()
            .Include(scoreDto => scoreDto.Player)
            .FirstOrDefaultAsync(s =>
                s.MapId == beatmap.Id
                && !s.IsRestricted
                && s.PlayerId == playerId
                && s.Mode == (int)mode
                && s.Mods == (int)mods.ToLegacyMods()
                && s.Status >= (int)SubmissionStatus.BestWithMods);
        
        return score == null ? null : new ApiScore(score, score.Player, beatmap);
    }

    public async Task SetScoreLeaderboardPosition(
        ApiScore score,
        bool withMods,
        Beatmap beatmap,
        List<ApiMod>? mods = null
    ) {
        score.LeaderboardPosition = await DbContext.Scores
            .Include(s => s.Player)
            .Where(s =>
                s.MapId == beatmap.Id
                && s.Mode == score.RulesetId
                && (withMods
                    ? s.Status >= (int)SubmissionStatus.BestWithMods
                    : s.Status == (int)SubmissionStatus.Best)
                && (!withMods || s.Mods == (int)score.LegacyMods)
                && (s.Player.Privileges & 1) == 1
                && !s.IsRestricted
                && (OrderByPp((GameMode)score.RulesetId)
                    ? score.Pp < s.PP
                    : score.TotalScore < s.LegacyTotalScore))
            .CountAsync() + 1;
    }

    public async Task<(List<ApiScore>, int, ApiScore?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        List<ApiMod> mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        Beatmap beatmap
    ) {
        var mapId = beatmap.Id;
        
        var leaderboard = (await GetBeatmapLeaderboardInternal(
                mode,
                type,
                mods.ToLegacyMods(), //TODO
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