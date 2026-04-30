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

    public async Task RemoveScore(
        long id
    ) {
        await DbContext.Scores
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync();
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
            .AsNoTracking()
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

    public async Task ToggleScoreReplayAvailability(
        long scoreId
    ) {
        await DbContext.Scores
            .Where(s => s.Id == scoreId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.HasReplay, true));
    }

    public async Task<List<ScoreDto>> GetPlayerRecentScores(
        int playerId,
        GameMode mode,
        int start = 0,
        int count = 50
    ) {
        return await DbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
                .ThenInclude(b => b.Beatmapset)
            .Where(s => s.PlayerId == playerId
                        && s.Mode == (int)mode
                        && s.PlayTime > DateTime.UtcNow.AddHours(-24))
            .OrderByDescending(s => s.PlayTime)
            .Skip(start)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> PlayerRecentScoresCount(
        int playerId,
        GameMode mode
    ) {
        return await DbContext.Scores
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId
                        && s.Mode == (int)mode
                        && s.PlayTime > DateTime.UtcNow.AddHours(-24))
            .CountAsync();
    }

    public async Task<List<ScoreDto>> GetPlayerFirstPlaceScores(
        int playerId,
        GameMode mode,
        int start = 0,
        int count = 50
    ) {
        return await DbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
                .ThenInclude(b => b.Beatmapset)
            .Where(s => s.PlayerId == playerId
                        && s.Mode == (int)mode
                        && s.Status == (int)SubmissionStatus.Best
                        && s.Ranked)
            .Where(s =>
                !DbContext.Scores.Any(o =>
                    o.PlayerId == playerId
                    && o.Mode == (int)mode
                    && o.Status == (int)SubmissionStatus.Best
                    && o.Ranked
                    && o.MapId == s.MapId
                    && (OrderByPp(mode)
                        ? o.PP > s.PP
                        : o.LegacyTotalScore > s.LegacyTotalScore)))
            .OrderByDescending(s => s.PlayTime)
            .Skip(start)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> PlayerFirstPlaceScoresCount(
        int playerId,
        GameMode mode
    ) {
        return await DbContext.Scores
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId
                        && s.Mode == (int)mode
                        && s.Status == (int)SubmissionStatus.Best
                        && s.Ranked)
            .Where(s =>
                !DbContext.Scores.Any(o =>
                    o.PlayerId == playerId
                    && o.Mode == (int)mode
                    && o.Status == (int)SubmissionStatus.Best
                    && o.Ranked
                    && o.MapId == s.MapId
                    && (OrderByPp(mode)
                        ? o.PP > s.PP
                        : o.LegacyTotalScore > s.LegacyTotalScore)))
            .CountAsync();
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
        
        var query = DbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .AsQueryable();
        
        if (mapId.HasValue)
            query = query.Where(s => s.MapId == mapId.Value);
        else if (!string.IsNullOrEmpty(md5))
            query = query.Where(s => s.BeatmapMD5 == md5);
        else
            throw new ArgumentException("Either mapId or md5 must be provided.");
        
        //TODO start using only s.IsRestricted instead of privileges
        query = query.Where(s => s.Mode == (int)mode
                         && (s.Player.Privileges & 1) == 1
                         && !s.IsRestricted);
        
        query = withMods
            ? query.Where(s => s.Status >= (int)SubmissionStatus.BestWithMods
                           && s.Mods == (int)mods)
            : query.Where(s => s.Status == (int)SubmissionStatus.Best);

        if (isCountry)
            query = query.Where(s => s.Player.Country == country);
        
        if (withFriendsList && friendIds.Count > 0)
            query = query.Where(s => friendIds.Contains(s.PlayerId));
        
        query = ApplyOrder(query, mode);

        var result = await query
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
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
                .ThenInclude(b => b.Beatmapset)
            .Where(s => s.PlayerId == playerId
                        && !s.IsRestricted
                        && s.Ranked
                        && s.Status == (int)SubmissionStatus.Best
                        && s.Mode == (int)mode)
            .OrderByDescending(s => s.PP)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }
    
    public async Task<List<ScoreDto>> GetBestScores(
        GameMode mode,
        int skip = 0,
        int count = 50
    ) {
        return await DbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
            .Where(s => s.Mode == (int)mode
                        && s.Ranked
                        && !s.IsRestricted
                        && s.Status == (int)SubmissionStatus.Best
            )
            .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.LegacyTotalScore)
            .Skip(skip)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ScoreDto>> GetRecentScores(
        GameMode mode,
        int skip = 0,
        int count = 50
    ) {
        return await DbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
                .ThenInclude(s => s.Beatmapset)
            .Where(s => s.Mode == (int)mode
                        && s.Ranked
                        && !s.IsRestricted
            )
            .OrderByDescending(s => s.PlayTime)
            .Skip(skip)
            .Take(count)
            .ToListAsync();
    }

    protected static bool OrderByPp(GameMode mode) => mode >= GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
    
    protected static IOrderedQueryable<ScoreDto> ApplyOrder(IQueryable<ScoreDto> q, GameMode mode)
    {
        return OrderByPp(mode) ? q.OrderByDescending(s => s.PP) : q.OrderByDescending(s => s.LegacyTotalScore);
    }
}