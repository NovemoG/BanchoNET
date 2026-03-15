using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class ScoresRepository(BanchoDbContext dbContext) : IScoresRepository
{
    public async Task<Score> InsertScore(
        Score score,
        bool isPlayerRestricted,
        string md5,
        int mapId
    ) {
        var dbScore = dbContext.Scores
            .Add(new ScoreDto
            {
                BeatmapMD5 = md5,
                MapId = mapId,
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
                OnlineChecksum = score.ClientChecksum,
                IsRestricted = isPlayerRestricted
            });
        await dbContext.SaveChangesAsync();

        score.Id = dbScore.Entity.Id;
        return score;
    }
    
    public async Task<ApiScore> InsertScore(
        ApiScore score,
        bool isPlayerRestricted,
        string md5,
        int mapId
    ) {
        var stats = score.Statistics;
        
        var dbScore = dbContext.Scores
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
                Grade = (byte)Enum.Parse<Grade>(score.Rank, true),
                Status = (byte)score.Status,
                Mode = (byte)score.RulesetId,
                StartTime = score.StartedAt,
                PlayTime = score.EndedAt,
                PlayerId = score.UserId,
                LegacyPerfect = score.LegacyPerfect,
                IsRestricted = isPlayerRestricted
            });
        await dbContext.SaveChangesAsync();

        score.Id = dbScore.Entity.Id;
        return score;
    }

    public async Task<ScoreDto?> GetScore(long id)
    {
        return await dbContext.Scores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> ScoreExists(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return false;

        return await dbContext.Scores
            .AsNoTracking()
            .AnyAsync(s => s.OnlineChecksum == checksum);
    }

    public async Task<Score?> GetPlayerRecentScore(int playerId)
    {
        var score = await dbContext.Scores
            .AsNoTracking()
            .OrderByDescending(s => s.PlayTime)
            .FirstOrDefaultAsync(s => s.PlayerId == playerId);

        return score == null ? null : new Score(score);
    }
    
    public async Task<List<ScoreDto>> GetPlayerRecentScores(int playerId, int start, int count = 10)
    {
        return await dbContext.Scores
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.PlayTime)
            .Skip(start)
            .Take(count)
            .ToListAsync();
    }
    
    public async Task<List<ScoreDto>> GetMultiplayerScores(List<int> playerIds, DateTime finishDate)
    {
        return await dbContext.Scores
            .AsNoTracking()
            .Where(s => playerIds.Contains(s.PlayerId) && s.PlayTime > finishDate)
            .ToListAsync();
    }

    public async Task UpdateScoreStatus(Score? score)
    {
        if (score == null) return;

        await UpdateScoreStatus(score.Id, score.Status);
    }

    public async Task UpdateScoreStatus(long id, SubmissionStatus newStatus)
    {
        await dbContext.Scores
            .AsNoTracking()
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Status, (int)newStatus));
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
        var score = await dbContext.Scores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => mapId.HasValue
                                    ? s.MapId == mapId
                                    : s.BeatmapMD5 == md5
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
        var score = await dbContext.Scores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => mapId.HasValue
                                        ? s.MapId == mapId
                                        : s.BeatmapMD5 == md5
                                      && s.PlayerId == playerId
                                      && s.Mode == (int)mode
                                      && s.Mods == (int)mods
                                      && s.Status >= (int)SubmissionStatus.BestWithMods);
        
        return score == null ? null : new Score(score);
    }

    public async Task<ScoreDto?> GetBestBeatmapScore(
        int mapId,
        GameMode mode
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Where(s => s.MapId == mapId
                        && s.Mode == (int)mode
                        && s.Status == (int)SubmissionStatus.Best
                        && (s.Player.Privileges & 1) == 1
                        && !s.IsRestricted)
            .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.LegacyTotalScore)
            .FirstOrDefaultAsync();
    }
    
    public async Task<ScoreDto?> GetBestBeatmapScore(
        string md5,
        GameMode mode
    ) {
        return await dbContext.Scores
            .Include(s => s.Player)
            .Where(s => s.BeatmapMD5 == md5
                        && s.Mode == (int)mode
                        && s.Status == (int)SubmissionStatus.Best
                        && (s.Player.Privileges & 1) == 1
                        && !s.IsRestricted)
            .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.LegacyTotalScore)
            .FirstOrDefaultAsync();
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
        score.LeaderboardPosition = await dbContext.Scores
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
	
    public Task<List<ScoreDto>> GetBeatmapLeaderboard(
        GameMode mode,
        LeaderboardType type,
        LegacyMods mods,
        string country,
        HashSet<int> playerIds,
        string md5
    ) => GetBeatmapLeaderboardInternal(mode, type, mods, country, playerIds, mapId: null, md5: md5);

    public Task<List<ScoreDto>> GetBeatmapLeaderboard(
        GameMode mode,
        LeaderboardType type,
        LegacyMods mods,
        string country,
        HashSet<int> playerIds,
        int mapId
    ) => GetBeatmapLeaderboardInternal(mode, type, mods, country, playerIds, mapId: mapId, md5: null);
    
    private async Task<List<ScoreDto>> GetBeatmapLeaderboardInternal(
        GameMode mode,
        LeaderboardType type,
        LegacyMods mods,
        string country,
        HashSet<int> playerIds,
        int? mapId,
        string? md5
    ) {
        var isCountry = type == LeaderboardType.Country;
        var withMods = type is LeaderboardType.Mods or LeaderboardType.CountryMods or LeaderboardType.FriendsMods;
        var withFriendsList = type == LeaderboardType.Friends;
        var friendIds = withFriendsList ? playerIds : [];
        
        var q = dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .AsQueryable();
        
        if (mapId.HasValue)
            q = q.Where(s => s.MapId == mapId.Value);
        else if (!string.IsNullOrEmpty(md5))
            q = q.Where(s => s.BeatmapMD5 == md5);
        else
            throw new ArgumentException("Either mapId or md5 must be provided.");
        
        q = q.Where(s => s.Mode == (int)mode
                         && (s.Player.Privileges & 1) == 1
                         && !s.IsRestricted);

        q = withMods
            ? q.Where(s => s.Status >= (int)SubmissionStatus.BestWithMods
                           && s.Mods == (int)mods)
            : q.Where(s => s.Status == (int)SubmissionStatus.Best);

        if (isCountry)
            q = q.Where(s => s.Player.Country == country);
        
        if (withFriendsList && friendIds.Count > 0)
            q = q.Where(s => friendIds.Contains(s.PlayerId));
        
        q = ApplyOrder(q, mode);

        var result = await q
            .Take(AppSettings.ScoresOnLeaderboard)
            .ToListAsync();
        
        /*TODO best we've managed to squeeze out of our brains is <1s with 5mln scores
               we're not knowledgeable enough to make it faster ~Cossin & foksurek*/

        return result;
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
    
    public async Task ToggleBeatmapScoresVisibility(int mapId, bool visible)
    {
        await dbContext.Scores
            .Where(s => s.MapId == mapId)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.IsRestricted, visible));
    }

    public async Task ToggleBeatmapScoresVisibility(string md5, bool visible)
    {
        await dbContext.Scores
            .Where(s => s.BeatmapMD5 == md5)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.IsRestricted, visible));
    }
    
    /// <summary>
    /// Deletes all scores with status not flagged as best older than 2 days from when the method was used.
    /// </summary>
    /// <returns>List of IDs of scores that were affected.</returns>
    public async Task<List<long>> DeleteOldScores(short differenceInHours = 48)
    {
        //TODO maybe instead of deleting just move to other table so we can keep the data?
        var date = DateTime.UtcNow - TimeSpan.FromHours(differenceInHours);

        // Saving IDs of scores that are not failed (we don't store failed scores replays)
        var scoreIds = await dbContext.Scores
            .Where(s => s.PlayTime < date
                        && s.Status == (int)SubmissionStatus.Submitted)
            .Select(s => s.Id)
            .ToListAsync();

        var affected = await dbContext.Scores
            .Where(s => s.Status <= (int)SubmissionStatus.Submitted
                        && s.PlayTime < date)
            .ExecuteDeleteAsync();
        // Deleting scores
/*#pragma warning disable EF1002
        var affected = await dbContext.Database.ExecuteSqlRawAsync(
            $"DELETE FROM Scores WHERE PlayTime < TIMESTAMP(\"{date:yyyy-MM-dd}\", \"{date:HH:mm:ss}\") " +
            $"AND Status < {(int)SubmissionStatus.BestWithMods}");
#pragma warning restore EF1002*/
        Console.WriteLine($"[BackgroundTasks] Deleted {affected} scores.");
        
        return scoreIds;
    }
    
    private static bool OrderByPp(GameMode mode) => mode >= GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
    
    private static IOrderedQueryable<ScoreDto> ApplyOrder(IQueryable<ScoreDto> q, GameMode mode)
    {
        return OrderByPp(mode) ? q.OrderByDescending(s => s.PP) : q.OrderByDescending(s => s.LegacyTotalScore);
    }
}