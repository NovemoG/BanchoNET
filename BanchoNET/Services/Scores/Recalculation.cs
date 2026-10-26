using System.Diagnostics;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;
using Microsoft.EntityFrameworkCore;
using Pp;

namespace BanchoNET.Services.Scores;

public sealed partial class ScoreService
{
    private async Task UpdateScoreGroup(
        int playerId,
        int mapId,
        GameMode mode,
        long? removeId = null,
        long? recalculateId = null
    ) {
        var beatmap = await beatmaps.GetBeatmap(mapId)
                  ?? throw new InvalidOperationException($"Beatmap {mapId} was not found.");

        var checksum = beatmap.Checksum;
        if (recalculateId != null)
        {
            var file = await beatmaps.EnsureLocalBeatmapFile(beatmap);
            if (!file.Available)
                throw new InvalidOperationException($"Cannot recalculate scores: beatmap file {mapId} is unavailable.");

            checksum = file.Checksum;
        }

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await ScoreProcessing.LockPlayer(db, playerId, mode);
            await ProcessScoreGroup(playerId, mapId, mode, checksum, removeId, recalculateId);
            await RecalculateStatistics(playerId, mode);

            await transaction.CommitAsync();
        }
        await RefreshRank(playerId, mode);
    }
    
    private async Task<int> ProcessScoreGroup(
        int playerId,
        int mapId,
        GameMode mode,
        string checksum,
        long? removeId = null,
        long? recalculateId = null,
        bool recalculateAll = false,
        RecalculationTimings? timings = null
    ) {
        var started = Stopwatch.GetTimestamp();

        var key = (long)playerId << 32 | (uint)mapId;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})");

        var validScores = await db.Scores
            .Include(s => s.Beatmap)
            .Where(s => s.PlayerId == playerId
                        && s.MapId == mapId
                        && s.Mode == mode
                        && (s.BeatmapMD5 == checksum && s.Status != SubmissionStatus.Failed
                            || s.Id == removeId
                            || s.Id == recalculateId))
            .ToListAsync();

        var candidates = validScores.Where(s => s.BeatmapMD5 == checksum).ToList();
        var visible = candidates.Any(s => s.Status >= SubmissionStatus.BestWithMods || s.TotalScoreStatus >= SubmissionStatus.BestWithMods);

        timings?.Loading += Stopwatch.GetElapsedTime(started);
        started = Stopwatch.GetTimestamp();

        var recalculated = 0;
        foreach (var score in validScores)
        {
            if (score.Id == removeId)
            {
                db.Scores.Remove(score);
                candidates.Remove(score);
            }
            else if (recalculateAll && score.Status != SubmissionStatus.Failed || score.Id == recalculateId)
            {
                score.CalculatePerformance(score.Beatmap);
                score.PP = MathF.Round(score.PP, 3, MidpointRounding.AwayFromZero);
                recalculated++;
            }
        }
        if (visible) ScoreBestSelection.Assign(candidates);

        timings?.Performance += Stopwatch.GetElapsedTime(started);
        started = Stopwatch.GetTimestamp();

        await db.SaveChangesAsync();

        timings?.Saving += Stopwatch.GetElapsedTime(started);
        return recalculated;
    }

    private async Task RecalculateStatistics(
        int playerId,
        GameMode mode
    ) {
        if (await db.Stats.SingleOrDefaultAsync(s => s.PlayerId == playerId && s.Mode == (byte)mode) is not { } stats)
            return;

        await db.Entry(stats).ReloadAsync();
        await ScoreStatistics.Recalculate(db, stats);
    }

    public async Task RemoveScore(
        long id
    ) {
        if (await scores.GetScore(id) is { } score)
            await UpdateScoreGroup(score.PlayerId, score.MapId, score.Mode, removeId: id);
    }

    public async Task<bool> RecalculateScore(
        long id
    ) {
        var score = await scores.GetScore(id);
        if (score == null) return false;
        
        await UpdateScoreGroup(score.PlayerId, score.MapId, score.Mode, recalculateId: id);
        return true;
    }

    public async Task RecalculateAllScores()
    {
        Log("Recalculating all scores");
        var total = Stopwatch.StartNew();
        var step = Stopwatch.StartNew();
        
        var groups = await db.Scores
            .Where(s => s.Status >= SubmissionStatus.BestWithMods || s.TotalScoreStatus >= SubmissionStatus.BestWithMods)
            .Select(s => new { s.PlayerId, s.MapId, s.Mode })
            .Distinct()
            .OrderBy(g => g.MapId).ThenBy(g => g.PlayerId).ThenBy(g => g.Mode)
            .ToListAsync();

        LogStep($"Loaded {groups.Count} score groups", step);

        step.Restart();
        
        var mapIds = groups.Select(g => g.MapId).Distinct().ToList();
        var maps = await db.Beatmaps
            .AsNoTracking()
            .Where(b => mapIds.Contains(b.Id))
            .Select(b => new { b.Id, b.SetId, b.MD5 })
            .ToListAsync();

        var checksums = new Dictionary<int, string>();
        var statuses = new Dictionary<BeatmapFileStatus, int>();
        
        foreach (var map in maps)
        {
            var file = await beatmaps.EnsureLocalBeatmapFile(map.Id, map.SetId, map.MD5);
            if (file.Available) checksums[map.Id] = file.Checksum;

            statuses[file.Status] = statuses.GetValueOrDefault(file.Status) + 1;
        }

        LogStep($"Checked {maps.Count} beatmap files ({string.Join(", ", statuses.Select(s => $"{s.Value} {s.Key}"))})", step);

        var timings = new RecalculationTimings();
        var affectedPlayers = new HashSet<(int PlayerId, GameMode Mode)>();
        int recalculated = 0, unavailable = 0, failed = 0;

        using (PpMethods.CacheDifficulty())
        {
            step.Restart();

            foreach (var group in groups)
            {
                affectedPlayers.Add((group.PlayerId, group.Mode));

                if (!checksums.TryGetValue(group.MapId, out var checksum))
                {
                    unavailable++;
                    continue;
                }

                try
                {
                    await using var transaction = await db.Database.BeginTransactionAsync();

                    await ScoreProcessing.LockPlayer(db, group.PlayerId, group.Mode);
                    recalculated += await ProcessScoreGroup(group.PlayerId, group.MapId, group.Mode, checksum, recalculateAll: true, timings: timings);

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    logger.LogError($"Failed to recalculate scores of player {group.PlayerId} on beatmap {group.MapId}", e, caller: nameof(ScoreService));
                    failed++;
                }

                // Saving scans every tracked entity, so keeping finished groups tracked would slow down each next one
                db.ChangeTracker.Clear();
            }

            LogStep($"Recalculated {recalculated} scores in {groups.Count - unavailable - failed} score groups "
                    + $"({unavailable} with unavailable beatmaps, {failed} failed; {timings})", step);
        }
        
        step.Restart();

        foreach (var (playerId, mode) in affectedPlayers)
        {
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                await ScoreProcessing.LockPlayer(db, playerId, mode);
                await RecalculateStatistics(playerId, mode);
                await transaction.CommitAsync();
            }

            db.ChangeTracker.Clear();
        }

        LogStep($"Recalculated statistics of {affectedPlayers.Count} players", step);
        step.Restart();

        foreach (var (playerId, mode) in affectedPlayers)
        {
            await RefreshRank(playerId, mode);
            db.ChangeTracker.Clear();
        }

        LogStep($"Refreshed ranks of {affectedPlayers.Count} players", step);
        LogStep("Finished recalculating all scores", total);
    }

    private void Log(
        string message
    ) {
        logger.LogInfo(message, nameof(ScoreService));
    }

    private void LogStep(
        string message,
        Stopwatch stopwatch
    ) {
        logger.LogInfo($"{message} in {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    private sealed class RecalculationTimings
    {
        public TimeSpan Loading;
        public TimeSpan Performance;
        public TimeSpan Saving;

        public override string ToString()
        {
            return $"loading {Loading.TotalSeconds:F2}s, pp {Performance.TotalSeconds:F2}s, saving {Saving.TotalSeconds:F2}s";
        }
    }
}