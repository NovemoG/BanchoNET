using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class LegacyScoresRepository(BanchoDbContext dbContext) : ScoresRepository(dbContext), ILegacyScoresRepository
{
    public async Task<Score> InsertScore(
        Score score,
        bool isPlayerRestricted,
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
                PP = score.PP,
                Acc = score.Acc,
                LegacyTotalScore = score.TotalScore,
                MaxCombo = score.MaxCombo,
                Mods = (int)score.Mods,
                Count300 = score.Count300,
                Count100 = score.Count100,
                Count50 = score.Count50,
                Misses = score.Misses,
                Gekis = score.Gekis,
                Katus = score.Katus,
                Grade = (byte)score.Grade,
                Status = (byte)score.Status,
                Mode = (byte)score.Mode,
                PlayTime = score.ClientTime,
                TimeElapsed = score.TimeElapsed,
                ClientFlags = (int)score.ClientFlags,
                PlayerId = score.PlayerId,
                LegacyPerfect = score.Perfect,
                IsPerfectCombo = score.Perfect,
                OnlineChecksum = score.ClientChecksum,
                IsRestricted = isPlayerRestricted
            });
        await DbContext.SaveChangesAsync();

        score.Id = dbScore.Entity.Id;
        return score;
    }

    public async Task<Score?> GetPlayerRecentScore(int playerId)
    {
        var score = await DbContext.Scores
            .AsNoTracking()
            .OrderByDescending(s => s.PlayTime)
            .FirstOrDefaultAsync(s => s.PlayerId == playerId);

        return score == null ? null : new Score(score);
    }
    
    public async Task UpdateScoreStatus(Score? score)
    {
        if (score == null) return;

        await UpdateScoreStatus(score.Id, score.Status);
    }
    
    public Task<Score?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        int mapId
    ) => GetPlayerBestScoreOnMapInternal(playerId, mode, mapId: mapId, md5: null);

    public Task<Score?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        string md5
    ) => GetPlayerBestScoreOnMapInternal(playerId, mode, md5: md5, mapId: null);
    
    private async Task<Score?> GetPlayerBestScoreOnMapInternal(
        int playerId,
        GameMode mode,
        int? mapId,
        string? md5
    ) {
        var score = await DbContext.Scores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => mapId.HasValue
                                    ? s.MapId == mapId
                                    : s.BeatmapMD5 == md5
                                  && !s.IsRestricted
                                  && s.PlayerId == playerId
                                  && s.Mode == (int)mode
                                  && s.Status == (int)SubmissionStatus.Best);
        
        return score == null ? null : new Score(score);
    }
    
    public Task<Score?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        LegacyMods mods,
        int mapId
    ) => GetPlayerBestScoreWithModsOnMapInternal(playerId, mode, mods, mapId: mapId, md5: null);

    public Task<Score?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        LegacyMods mods,
        string md5
    ) => GetPlayerBestScoreWithModsOnMapInternal(playerId, mode, mods, md5: md5, mapId: null);
    
    private async Task<Score?> GetPlayerBestScoreWithModsOnMapInternal(
        int playerId,
        GameMode mode,
        LegacyMods mods,
        int? mapId,
        string? md5
    ) {
        var score = await DbContext.Scores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => mapId.HasValue
                                        ? s.MapId == mapId
                                        : s.BeatmapMD5 == md5
                                      && !s.IsRestricted
                                      && s.PlayerId == playerId
                                      && s.Mode == (int)mode
                                      && s.Mods == (int)mods
                                      && s.Status >= (int)SubmissionStatus.BestWithMods);
        
        return score == null ? null : new Score(score);
    }
    
    public Task SetScoreLeaderboardPosition(
        Score score,
        bool withMods,
        string md5,
        LegacyMods mods = LegacyMods.None
    ) => SetScoreLeaderboardPositionInternal(score, withMods, md5: md5, mapId: null, mods: mods);

    public Task SetScoreLeaderboardPosition(
        Score score,
        bool withMods,
        int mapId,
        LegacyMods mods = LegacyMods.None
    ) => SetScoreLeaderboardPositionInternal(score, withMods, mapId: mapId, md5: null, mods: mods);

    private async Task SetScoreLeaderboardPositionInternal(
        Score score,
        bool withMods,
        int? mapId,
        string? md5,
        LegacyMods mods = LegacyMods.None
    ) {
        score.LeaderboardPosition = await DbContext.Scores
            .Include(s => s.Player)
            .Where(s => mapId.HasValue
                ? s.MapId == mapId
                : s.BeatmapMD5 == md5
                  && s.Mode == (int)score.Mode
                  && (withMods
                      ? s.Status >= (int)SubmissionStatus.BestWithMods
                      : s.Status == (int)SubmissionStatus.Best)
                  && (!withMods || s.Mods == (int)mods) 
                  && (s.Player.Privileges & 1) == 1 
                  && !s.IsRestricted
                  && (OrderByPp(score.Mode)
                      ? score.PP < s.PP
                      : score.TotalScore < s.LegacyTotalScore))
            .CountAsync() + 1;
    }
    
    public Task<(List<ScoreDto>, Score?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        LegacyMods mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        string md5
    ) => GetLeaderboardScoresInternal(type, mode, mods, playerId, country, friendIds, md5: md5, mapId: null);

    public Task<(List<ScoreDto>, Score?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        LegacyMods mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        int mapId
    ) => GetLeaderboardScoresInternal(type, mode, mods, playerId, country, friendIds, mapId: mapId, md5: null);
    
    private async Task<(List<ScoreDto>, Score?)> GetLeaderboardScoresInternal(
        LeaderboardType type,
        GameMode mode,
        LegacyMods mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        int? mapId,
        string? md5
    ) {
        var leaderboard = await GetBeatmapLeaderboardInternal(mode, type, mods, country, friendIds, mapId, md5);
		
        Score? playerBest = null;
        if (leaderboard.Count > 0)
        {
            var withMods = type is LeaderboardType.Mods or LeaderboardType.CountryMods or LeaderboardType.FriendsMods;
            playerBest = withMods
                ? await GetPlayerBestScoreWithModsOnMapInternal(playerId, mode, mods, mapId, md5)
                : await GetPlayerBestScoreOnMapInternal(playerId, mode, mapId, md5);
			
            if (playerBest != null)
                await SetScoreLeaderboardPositionInternal(playerBest, withMods, mapId, md5, mods);
        }
		
        return (leaderboard, playerBest);
    }
}