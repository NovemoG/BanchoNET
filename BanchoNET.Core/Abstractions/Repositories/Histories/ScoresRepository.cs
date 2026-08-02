using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
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

    public async Task<bool> RecalculateScore(
        long id
    ) {
        var score = await DbContext.Scores
            .Include(s => s.Beatmap)
            .FirstOrDefaultAsync(s => s.Id == id);
        
        if (score == null) return false;
        
        score.RecalculatePerformance(score.Beatmap);
        await DbContext.SaveChangesAsync();
        
        return true;
    }

    public async Task RecalculateAllScores() {
        var validScores = await DbContext.Scores
            .Include(s => s.Beatmap)
            .Where(s => s.Status >= SubmissionStatus.BestWithMods)
            .ToListAsync();
        
        foreach (var score in validScores) {
            score.RecalculatePerformance(score.Beatmap);
        }
        
        Logger.Shared.LogInfo($"Recalculated pp for {validScores.Count} scores");
        
        await DbContext.SaveChangesAsync();
    }
    
    /// <summary>
    /// Ages out scores that never made a leaderboard, according to
    /// <see cref="AppSettings.ScoreRetentionMode"/>.
    /// <para>
    /// Anything attached to a multiplayer game is exempt in every mode - a match scoreboard must
    /// keep pointing at real scores.
    /// </para>
    /// </summary>
    /// <returns>IDs whose replay files should be removed from disk.</returns>
    public async Task<List<long>> PurgeOldScores()
    {
        var mode = AppSettings.ScoreRetentionMode;
        var date = DateTime.UtcNow - TimeSpan.FromHours(AppSettings.ScoreRetentionHours);

        var expired = DbContext.Scores
            .Where(s => s.PlayTime < date && !DbContext.MultiplayerScores.Any(ms => ms.ScoreId == s.Id));

        // Only passed scores have a replay file on disk
        var replayIds = mode == ScoreRetentionMode.Keep
            ? []
            : await expired
                .AsNoTracking()
                .Where(s => s.Status == SubmissionStatus.Submitted)
                .Select(s => s.Id)
                .ToListAsync();

        var deleted = mode == ScoreRetentionMode.Delete
            ? await expired.Where(s => s.Status <= SubmissionStatus.Submitted).ExecuteDeleteAsync()
            : await expired.Where(s => s.Status == SubmissionStatus.Failed).ExecuteDeleteAsync();

        Logger.Shared.LogInfo(
            $"Score retention ({mode}): deleted {deleted} scores, {replayIds.Count} replays to remove",
            caller: "BackgroundTasks"
        );

        return replayIds;
    }
    
    public async Task ToggleBeatmapScoresVisibility(int mapId)
    {
        await DbContext.Scores
            .Where(s => s.MapId == mapId) // changed to submitted so that the replay is also removed
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Status, SubmissionStatus.Submitted));
    }

    public async Task ToggleBeatmapScoresVisibility(string md5)
    {
        await DbContext.Scores
            .Where(s => s.BeatmapMD5 == md5) // changed to submitted so that the replay is also removed
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Status, SubmissionStatus.Submitted));
    }

    public async Task ToggleScoreReplayAvailability(
        long scoreId
    ) {
        await DbContext.Scores
            .Where(s => s.Id == scoreId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.HasReplay, true));
    }

    public async Task SetBeatmapScoresRankedStatus(
        int mapId,
        bool ranked
    ) {
        await DbContext.Scores
            .Where(s => s.MapId == mapId)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Ranked, ranked));
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
                        && s.Mode == mode
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
                        && s.Mode == mode
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
                        && (s.Player.Privileges & 1) == 1
                        && s.Mode == mode
                        && s.Status == SubmissionStatus.Best
                        && s.Beatmap.Status >= BeatmapStatus.Ranked)
            .Where(s =>
                !DbContext.Scores.Any(o =>
                    o.MapId == s.MapId
                    && (o.Player.Privileges & 1) == 1
                    && o.Mode == mode
                    && o.Status == SubmissionStatus.Best
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
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
            .Where(s => s.PlayerId == playerId
                        && (s.Player.Privileges & 1) == 1
                        && s.Mode == mode
                        && s.Status == SubmissionStatus.Best
                        && s.Beatmap.Status >= BeatmapStatus.Ranked)
            .Where(s =>
                !DbContext.Scores.Any(o =>
                    o.MapId == s.MapId
                    && (o.Player.Privileges & 1) == 1
                    && o.Mode == mode
                    && o.Status == SubmissionStatus.Best
                    && (OrderByPp(mode)
                        ? o.PP > s.PP
                        : o.LegacyTotalScore > s.LegacyTotalScore)))
            .CountAsync();
    }

    public async Task UpdateScoreStatus(long id, SubmissionStatus newStatus)
    {
        await DbContext.Scores
            .AsNoTracking()
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Status, newStatus));
    }
    
    public async Task<ScoreDto?> GetBestBeatmapScore(
        int mapId,
        GameMode mode
    ) {
        return await DbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Where(s => s.MapId == mapId
                        && (s.Player.Privileges & 1) == 1
                        && s.Mode == mode
                        && s.Status == SubmissionStatus.Best)
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
                        && (s.Player.Privileges & 1) == 1
                        && s.Mode == mode
                        && s.Status == SubmissionStatus.Best)
            .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.LegacyTotalScore)
            .FirstOrDefaultAsync();
    }
    
    public Task<List<ScoreDto>> GetBeatmapLeaderboard(
        GameMode mode,
        LeaderboardType type,
        string mods,
        string country,
        HashSet<int> playerIds,
        string md5
    ) => GetBeatmapLeaderboardInternal(mode, type, mods, country, playerIds, mapId: null, md5: md5);

    public Task<List<ScoreDto>> GetBeatmapLeaderboard(
        GameMode mode,
        LeaderboardType type,
        string mods,
        string country,
        HashSet<int> playerIds,
        int mapId
    ) => GetBeatmapLeaderboardInternal(mode, type, mods, country, playerIds, mapId: mapId, md5: null);
    
    protected async Task<List<ScoreDto>> GetBeatmapLeaderboardInternal(
        GameMode mode,
        LeaderboardType type,
        string mods,
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
        
        query = query.Where(s => s.Mode == mode
                                 && (s.Player.Privileges & 1) == 1);

        query = withMods
            ? query.Where(s => s.Status >= SubmissionStatus.BestWithMods
                               && s.ModKeys == mods)
            : query.Where(s => s.Status == SubmissionStatus.Best);

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
                        && (s.Beatmap.Status == BeatmapStatus.Ranked || s.Beatmap.Status == BeatmapStatus.Approved)
                        && s.Status == SubmissionStatus.Best
                        && s.Mode == mode
                        && s.Ranked)
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
            .Where(s => s.Mode == mode
                        && (s.Player.Privileges & 1) == 1
                        && (s.Beatmap.Status == BeatmapStatus.Ranked || s.Beatmap.Status == BeatmapStatus.Approved)
                        && s.Status == SubmissionStatus.Best
                        && s.Ranked)
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
            .Where(s => s.Mode == mode
                        && (s.Player.Privileges & 1) == 1
                        && s.Beatmap.Status >= BeatmapStatus.Ranked)
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