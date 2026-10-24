using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Scores;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public sealed class ScoresRepository(BanchoDbContext dbContext) : IScoresRepository
{
    public async Task<ScoreLeaderboard> GetLeaderboardScores(
        int mapId,
        GameMode mode,
        LeaderboardType type,
        string mods,
        int playerId,
        string country,
        HashSet<int> friends,
        HashSet<int> teammates,
        bool representedByPp,
        LeaderboardSort sort,
        int limit
    ) {
        var withMods = type is LeaderboardType.Mods or LeaderboardType.CountryMods or LeaderboardType.FriendsMods or LeaderboardType.TeamMods;
        IQueryable<ScoreDto> query = Leaderboard(mapId, mode, representedByPp, withMods ? mods : null).AsNoTracking().Include(s => s.Player);
        
        if (type is LeaderboardType.Country or LeaderboardType.CountryMods)
            query = query.Where(s => s.Player.Country == country);
        
        if (type is LeaderboardType.Friends or LeaderboardType.FriendsMods)
        {
            var visible = friends.Append(playerId).ToHashSet();
            query = query.Where(s => visible.Contains(s.PlayerId));
        }
        
        if (type is LeaderboardType.Team or LeaderboardType.TeamMods)
            query = query.Where(s => teammates.Contains(s.PlayerId));
        
        var own = await query.FirstOrDefaultAsync(s => s.PlayerId == playerId);
        var position = own == null ? 0 : await LeaderboardOrdering.Ahead(query, own, sort).CountAsync() + 1;
        
        return new ScoreLeaderboard(await LeaderboardOrdering.Order(query, sort).Take(limit).ToListAsync(), await query.CountAsync(), own, position);
    }

    public async Task<ScoreDto?> GetPlayerRecentScore(
        int playerId
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.PlayTime)
            .ThenByDescending(s => s.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<ScoreDto?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        int mapId,
        bool performancePoints,
        string? mods = null
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Where(s => s.PlayerId == playerId
                        && s.Mode == mode
                        && s.MapId == mapId
                        && (mods == null
                            ? (performancePoints ? s.Status : s.TotalScoreStatus) == SubmissionStatus.Best
                            : (performancePoints ? s.Status : s.TotalScoreStatus) >= SubmissionStatus.BestWithMods && s.ModKeys == mods))
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetLeaderboardPosition(
        ScoreDto score,
        bool performancePoints,
        LeaderboardSort sort,
        string? mods = null
    ) {
        return await LeaderboardOrdering.Ahead(Leaderboard(score.MapId, score.Mode, performancePoints, mods), score, sort).CountAsync() + 1;
    }

    private IQueryable<ScoreDto> Leaderboard(
        int mapId,
        GameMode mode,
        bool performancePoints,
        string? mods
    ) {
        return dbContext.Scores
            .Where(s => s.MapId == mapId
                        && s.Mode == mode
                        && (s.Player.Privileges & 1) == 1
                        && (mods == null
                            ? (performancePoints ? s.Status : s.TotalScoreStatus) == SubmissionStatus.Best
                            : (performancePoints ? s.Status : s.TotalScoreStatus) >= SubmissionStatus.BestWithMods && s.ModKeys == mods));
    }

    public async Task<ScoreLeaderboardPreferences> GetLeaderboardPreferences(
        int playerId
    ) {
        return await dbContext.Players
            .Where(p => p.Id == playerId)
            .Select(p => new ScoreLeaderboardPreferences(
                p.LeaderboardPerformancePoints,
                p.LeaderboardSorting == LeaderboardSort.Score && p.LeaderboardPerformancePoints
                    ? LeaderboardSort.PP
                    : p.LeaderboardSorting))
            .SingleAsync();
    }

    public Task<bool> GetLeaderboardPerformancePoints(
        int playerId
    ) {
        return dbContext.Players
            .Where(p => p.Id == playerId)
            .Select(p => p.LeaderboardPerformancePoints)
            .SingleAsync();
    }

    public async Task<ScoreDto?> GetScore(
        long id
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
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

        var expired = dbContext.Scores
            .Where(s => (s.PlayTime < date || s.BeatmapMD5 != s.Beatmap.MD5)
                        && !s.IsPinned
                        && s.Status <= SubmissionStatus.Submitted
                        && s.TotalScoreStatus <= SubmissionStatus.Submitted 
                        && !dbContext.MultiplayerScores.Any(ms => ms.ScoreId == s.Id));

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
        await dbContext.Scores
            .Where(s => s.MapId == mapId && s.Status > SubmissionStatus.Failed)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Status, SubmissionStatus.Submitted)
                .SetProperty(s => s.TotalScoreStatus, SubmissionStatus.Submitted));
    }

    public async Task ToggleBeatmapScoresVisibility(string md5)
    {
        await dbContext.Scores
            .Where(s => s.BeatmapMD5 == md5 && s.Status > SubmissionStatus.Failed)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Status, SubmissionStatus.Submitted)
                .SetProperty(s => s.TotalScoreStatus, SubmissionStatus.Submitted));
    }

    public async Task RefreshScoresRankedStatus(
        IReadOnlyCollection<int> mapIds
    ) {
        if (mapIds.Count == 0) return;

        // passed scores on ranked or approved beatmaps give pp, restricted players are left out when stats and leaderboards are read
        var changed = await dbContext.Scores
            .Where(s => mapIds.Contains(s.MapId))
            .Where(s => s.Ranked != (s.Status > SubmissionStatus.Failed
                                     && (s.Beatmap.Status == BeatmapStatus.Ranked || s.Beatmap.Status == BeatmapStatus.Approved)))
            .Select(s => new { s.Id, s.PlayerId, s.Mode, Ranked = !s.Ranked })
            .ToListAsync();

        if (changed.Count == 0) return;

        foreach (var group in changed.GroupBy(s => s.Ranked))
        {
            var ids = group.Select(s => s.Id).ToList();

            await dbContext.Scores
                .Where(s => ids.Contains(s.Id))
                .ExecuteUpdateAsync(p => p.SetProperty(s => s.Ranked, group.Key));
        }

        await RecalculatePlayerStats(changed.Select(s => (s.PlayerId, s.Mode)));
    }

    public async Task RecalculatePlayerStats(
        IEnumerable<(int PlayerId, GameMode Mode)> players
    ) {
        foreach (var (playerId, mode) in players.Distinct())
        {
            var stats = await dbContext.Stats.SingleOrDefaultAsync(s => s.PlayerId == playerId && s.Mode == (byte)mode);
            if (stats == null) continue;

            await ScoreStatistics.Recalculate(dbContext, stats);
        }
    }

    public async Task<List<ScoreDto>> GetPlayerRecentScores(
        int playerId,
        GameMode mode,
        int start = 0,
        int count = 50,
        bool failed = false
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
                .ThenInclude(b => b.Beatmapset)
            .Where(s => s.PlayerId == playerId
                        && s.Mode == mode
                        && (failed || s.Status > SubmissionStatus.Failed)
                        && s.PlayTime > DateTime.UtcNow.AddHours(-24))
            .OrderByDescending(s => s.PlayTime)
            .Skip(start)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> PlayerRecentScoresCount(
        int playerId,
        GameMode mode,
        bool failed = false
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId
                        && s.Mode == mode
                        && (failed || s.Status > SubmissionStatus.Failed)
                        && s.PlayTime > DateTime.UtcNow.AddHours(-24))
            .CountAsync();
    }

    public async Task<List<ScoreDto>> GetPlayerFirstPlaceScores(
        int playerId,
        GameMode mode,
        int start = 0,
        int count = 50
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
                .ThenInclude(b => b.Beatmapset)
            .Where(s => s.PlayerId == playerId
                        && (s.Player.Privileges & 1) == 1
                        && s.Mode == mode
                        && (FirstPlacesByScore ? s.TotalScoreStatus : s.Status) == SubmissionStatus.Best
                        && s.Beatmap.Status >= BeatmapStatus.Ranked)
            .Where(s =>
                !dbContext.Scores.Any(o =>
                    o.MapId == s.MapId
                    && (o.Player.Privileges & 1) == 1
                    && o.Mode == mode
                    && (FirstPlacesByScore ? o.TotalScoreStatus : o.Status) == SubmissionStatus.Best
                    && (OrderByPp
                        ? o.PP > s.PP || o.PP == s.PP && (o.PlayTime < s.PlayTime || o.PlayTime == s.PlayTime && o.Id < s.Id)
                        : o.TotalScore > s.TotalScore || o.TotalScore == s.TotalScore && (o.PlayTime < s.PlayTime || o.PlayTime == s.PlayTime && o.Id < s.Id))))
            .OrderByDescending(s => s.PlayTime)
            .Skip(start)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> PlayerFirstPlaceScoresCount(
        int playerId,
        GameMode mode
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
            .Where(s => s.PlayerId == playerId
                        && (s.Player.Privileges & 1) == 1
                        && s.Mode == mode
                        && (FirstPlacesByScore ? s.TotalScoreStatus : s.Status) == SubmissionStatus.Best
                        && s.Beatmap.Status >= BeatmapStatus.Ranked)
            .Where(s =>
                !dbContext.Scores.Any(o =>
                    o.MapId == s.MapId
                    && (o.Player.Privileges & 1) == 1
                    && o.Mode == mode
                    && (FirstPlacesByScore ? o.TotalScoreStatus : o.Status) == SubmissionStatus.Best
                    && (OrderByPp
                        ? o.PP > s.PP || o.PP == s.PP && (o.PlayTime < s.PlayTime || o.PlayTime == s.PlayTime && o.Id < s.Id)
                        : o.TotalScore > s.TotalScore || o.TotalScore == s.TotalScore && (o.PlayTime < s.PlayTime || o.PlayTime == s.PlayTime && o.Id < s.Id))))
            .CountAsync();
    }

    public async Task<ScoreDto?> GetBestBeatmapScore(
        int mapId,
        GameMode mode
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Where(s => s.MapId == mapId
                        && (s.Player.Privileges & 1) == 1
                        && s.Mode == mode
                        && (FirstPlacesByScore ? s.TotalScoreStatus : s.Status) == SubmissionStatus.Best)
            .OrderByDescending(s => OrderByPp ? s.PP : s.TotalScore)
            .ThenBy(s => s.PlayTime).ThenBy(s => s.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<ScoreDto?> GetBestBeatmapScore(
        string md5,
        GameMode mode
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Where(s => s.BeatmapMD5 == md5
                        && (s.Player.Privileges & 1) == 1
                        && s.Mode == mode
                        && (FirstPlacesByScore ? s.TotalScoreStatus : s.Status) == SubmissionStatus.Best)
            .OrderByDescending(s => OrderByPp ? s.PP : s.TotalScore)
            .ThenBy(s => s.PlayTime).ThenBy(s => s.Id)
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<ScoreDto>> GetPlayerBestScores(
        int playerId,
        GameMode mode,
        int offset,
        int limit
    ) {
        return await dbContext.Scores
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
            .Take(PageSize(offset, limit, AppSettings.TopPlaysCount))
            .ToListAsync();
    }

    public async Task<List<ScoreDto>> GetPlayerBestScoresWithMods(
        int playerId,
        GameMode mode,
        int offset,
        int limit,
        string[] mods,
        bool matchExactly
    ) {
        return await PlayerBestScoresWithMods(playerId, mode, mods, matchExactly)
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
                .ThenInclude(b => b.Beatmapset)
            .OrderByDescending(s => s.PP)
            .Skip(offset)
            .Take(PageSize(offset, limit, AppSettings.FilteredTopPlaysCount))
            .ToListAsync();
    }

    public async Task<int> GetPlayerBestScoresWithModsCount(
        int playerId,
        GameMode mode,
        string[] mods,
        bool matchExactly
    ) {
        var count = await PlayerBestScoresWithMods(playerId, mode, mods, matchExactly)
            .AsNoTracking()
            .CountAsync();

        return Math.Min(count, AppSettings.FilteredTopPlaysCount);
    }

    /// <summary>
    /// Limits a page so that paging never goes past the first <paramref name="cap"/> scores.
    /// </summary>
    private static int PageSize(
        int offset,
        int limit,
        int cap
    ) => Math.Clamp(Math.Min(limit, cap - offset), 0, cap);

    private IQueryable<ScoreDto> PlayerBestScoresWithMods(
        int playerId,
        GameMode mode,
        string[] mods,
        bool matchExactly
    ) {
        var matching = dbContext.Scores
            .Where(s => s.PlayerId == playerId
                        && (s.Beatmap.Status == BeatmapStatus.Ranked || s.Beatmap.Status == BeatmapStatus.Approved)
                        && s.Status >= SubmissionStatus.BestWithMods
                        && s.Mode == mode
                        && s.Ranked);

        var modKeys = mods
            .Where(m => m != "NM")
            .Select(m => $";{m.ToUpper()};")
            .Distinct()
            .ToArray();
        
        foreach (var key in modKeys)
            matching = matching.Where(s => (";" + s.ModKeys).Contains(key));

        if (matchExactly)
        {
            // Key count makes sure the score has no other mods
            var modCount = modKeys.Length;
            matching = matching.Where(s => s.ModKeys.Length - s.ModKeys.Replace(";", "").Length == modCount);
        }

        // Multiple mod combinations can contain the requested mods, only the best of them is kept for each beatmap
        return matching.Where(s =>
            !matching.Any(o => o.MapId == s.MapId
                               && (o.PP > s.PP || o.PP == s.PP && (o.PlayTime < s.PlayTime || o.PlayTime == s.PlayTime && o.Id < s.Id))
            )
        );
    }
    
    public async Task<List<ScoreDto>> GetBestScores(
        GameMode mode,
        int skip = 0,
        int count = 50
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Beatmap)
            .Where(s => s.Mode == mode
                        && (s.Player.Privileges & 1) == 1
                        && (s.Beatmap.Status == BeatmapStatus.Ranked || s.Beatmap.Status == BeatmapStatus.Approved)
                        && s.Status == SubmissionStatus.Best
                        && s.Ranked)
            .OrderByDescending(s => s.PP)
            .Skip(skip)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetBestScoresCount(
        GameMode mode
    ) {
        return await dbContext.Scores
            .AsNoTracking()
            .CountAsync(s => s.Mode == mode
                             && (s.Player.Privileges & 1) == 1
                             && (s.Beatmap.Status == BeatmapStatus.Ranked || s.Beatmap.Status == BeatmapStatus.Approved)
                             && s.Status == SubmissionStatus.Best
                             && s.Ranked);
    }

    public async Task<List<ScoreDto>> GetRecentScores(
        GameMode mode,
        int skip = 0,
        int count = 50
    ) {
        return await dbContext.Scores
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

    private static bool OrderByPp => !AppSettings.CountFirstPlaceRanksByScore;
    private static bool FirstPlacesByScore => AppSettings.CountFirstPlaceRanksByScore;
}