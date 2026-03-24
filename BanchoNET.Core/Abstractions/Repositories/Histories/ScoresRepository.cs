using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Abstractions.Repositories.Histories;

public abstract class ScoresRepository(BanchoDbContext dbContext) : IScoresRepository
{
    protected readonly BanchoDbContext DbContext = dbContext;
    
    public async Task<ScoreDto?> GetScore(long id)
    {
        return await DbContext.Scores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }
    
    public async Task<bool> ScoreExists(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return false;

        return await DbContext.Scores
            .AsNoTracking()
            .AnyAsync(s => s.OnlineChecksum == checksum);
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
        var scoreIds = await DbContext.Scores
            .Where(s => s.PlayTime < date
                        && s.Status == (int)SubmissionStatus.Submitted)
            .Select(s => s.Id)
            .ToListAsync();

        var affected = await DbContext.Scores
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
    
    public async Task ToggleBeatmapScoresVisibility(int mapId, bool visible)
    {
        await DbContext.Scores
            .Where(s => s.MapId == mapId)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.IsRestricted, visible));
    }

    public async Task ToggleBeatmapScoresVisibility(string md5, bool visible)
    {
        await DbContext.Scores
            .Where(s => s.BeatmapMD5 == md5)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.IsRestricted, visible));
    }
    
    public async Task<List<ScoreDto>> GetPlayerRecentScores(int playerId, int start, int count = 10)
    {
        return await DbContext.Scores
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.PlayTime)
            .Skip(start)
            .Take(count)
            .ToListAsync();
    }
    
    public async Task<List<ScoreDto>> GetMultiplayerScores(List<int> playerIds, DateTime finishDate)
    {
        return await DbContext.Scores
            .AsNoTracking()
            .Where(s => playerIds.Contains(s.PlayerId) && s.PlayTime > finishDate)
            .ToListAsync();
    }
    
    public async Task UpdateScoreStatus(long id, SubmissionStatus newStatus)
    {
        await DbContext.Scores
            .AsNoTracking()
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Status, (int)newStatus));
    }
    
    public async Task<ScoreDto?> GetBestBeatmapScore(
        int mapId,
        GameMode mode
    ) {
        return await DbContext.Scores
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
        return await DbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Where(s => s.BeatmapMD5 == md5
                        && s.Mode == (int)mode
                        && s.Status == (int)SubmissionStatus.Best
                        && (s.Player.Privileges & 1) == 1
                        && !s.IsRestricted)
            .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.LegacyTotalScore)
            .FirstOrDefaultAsync();
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
    
    protected async Task<List<ScoreDto>> GetBeatmapLeaderboardInternal(
        GameMode mode,
        LeaderboardType type,
        LegacyMods mods,
        string country,
        HashSet<int> playerIds,
        int? mapId,
        string? md5
    ) {
        var isCountry = type == LeaderboardType.Country;
        var withMods = type is LeaderboardType.Mods or LeaderboardType.CountryMods or LeaderboardType.FriendsMods or LeaderboardType.TeamMods;
        var withFriendsList = type == LeaderboardType.Friends;
        var friendIds = withFriendsList ? playerIds : [];
        
        var q = DbContext.Scores
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
    
    public async Task<List<ScoreDto>> GetPlayerBestScores(
        int playerId,
        GameMode mode,
        int offset,
        int limit
    ) {
        return await DbContext.Scores
            .Where(s => s.PlayerId == playerId
                        && !s.IsRestricted
                        && s.Ranked
                        && s.Status == (int)SubmissionStatus.Best
                        && s.Mode == (int)mode)
            .OrderByDescending(s => s.PP)
            .Skip(offset)
            .Take(limit - offset)
            .OrderByDescending(s => s.PP)
            .ToListAsync();
    }
    
    protected static bool OrderByPp(GameMode mode) => mode >= GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
    
    protected static IOrderedQueryable<ScoreDto> ApplyOrder(IQueryable<ScoreDto> q, GameMode mode)
    {
        return OrderByPp(mode) ? q.OrderByDescending(s => s.PP) : q.OrderByDescending(s => s.LegacyTotalScore);
    }
}