using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Scores;
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
                LazerMods = score.ModsToString(),
                Count300 = stats.Great ?? 0,
                Count100 = stats.Ok ?? 0,
                Count50 = stats.Meh ?? 0,
                Misses = stats.Miss ?? 0,
                Gekis = stats.LargeTickHit ?? 0,
                Katus = stats.SliderTailHit ?? 0,
                IgnoreHit = stats.IgnoreHit ?? 0,
                IgnoreMiss = stats.IgnoreMiss ?? 0,
                Grade = (byte)score.Grade,
                Status = (byte)score.Status,
                Mode = (byte)score.RulesetId,
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

    public Task UpdateScoreStatus(
        ApiScore? score
    ) {
        throw new NotImplementedException();
    }

    public Task<ApiScore?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        int mapId
    ) {
        throw new NotImplementedException();
    }

    public Task<ApiScore?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        List<ApiMod> mods,
        int mapId
    ) {
        throw new NotImplementedException();
    }

    public Task SetScoreLeaderboardPosition(
        ApiScore score,
        bool withMods,
        int mapId,
        List<ApiMod>? mods = null
    ) {
        throw new NotImplementedException();
    }

    public async Task<(List<ApiScore>, int, ApiScore?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        List<ApiMod> mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        int mapId
    ) {
        throw new NotImplementedException();
    }
}