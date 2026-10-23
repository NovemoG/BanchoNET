using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Dependencies.Official;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps;

namespace BanchoNET.Core.Tools;

// TODO Temporary for manual migration
public static class ScoreMigration
{
    public static async Task Run(
        BanchoDbContext db,
        IBeatmapHandler beatmaps
    ) {
        var mapIds = await db.Scores
            .Select(s => s.MapId)
            .Distinct()
            .Order()
            .ToArrayAsync();
        
        var skipped = new List<int>();
        
        foreach (var mapId in mapIds)
        {
            Logger.Shared.LogInfo($"Standardising historical scores for beatmap {mapId}", nameof(ScoreMigration));
            
            var beatmap = await beatmaps.GetBeatmap(mapId);
            var stored = await db.Beatmaps.AsNoTracking().SingleAsync(b => b.Id == mapId);
            
            var workingBeatmap = beatmap != null && await beatmaps.EnsureLocalBeatmapFile(beatmap)
                ? BanchoWorkingBeatmap.FromFile(Storage.GetBeatmapPath(mapId), mapId)
                : null;
            
            if (workingBeatmap == null)
            {
                skipped.Add(mapId);
                Logger.Shared.LogWarning($"Beatmap {mapId} is unavailable, its scores are hidden.", caller: nameof(ScoreMigration));
            }
            
            await using var transaction = await db.Database.BeginTransactionAsync();
            
            var scores = await db.Scores.Include(s => s.Player).Where(s => s.MapId == mapId).ToListAsync();
            StandardiseMap(scores, beatmap?.Status ?? stored.Status, beatmap?.Checksum ?? stored.MD5, workingBeatmap);
            
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            db.ChangeTracker.Clear();
        }

        await MergeModes(db);

        if (bool.TryParse(Environment.GetEnvironmentVariable("SORT_LEADERBOARD_BY_PP"), out var ppDisplay))
            await db.Players.ExecuteUpdateAsync(p => p.SetProperty(v => v.LeaderboardPerformancePoints, ppDisplay));

        foreach (var stats in await db.Stats.ToListAsync())
            await ScoreStatistics.Recalculate(db, stats);

        Logger.Shared.LogInfo($"Standardised historical scores on {mapIds.Length - skipped.Count} beatmaps and rebuilt player statistics.", nameof(ScoreMigration));
        
        if (skipped.Count > 0)
            Logger.Shared.LogWarning($"Hid scores on {skipped.Count} unavailable beatmaps: {string.Join(", ", skipped)}", caller: nameof(ScoreMigration));
    }

    private static async Task MergeModes(
        BanchoDbContext db
    ) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        
        var stats = await db.Stats.ToListAsync();
        var merged = MergeStatistics(stats);
        
        db.Stats.AddRange(merged.Where(s => !stats.Contains(s)));
        db.Stats.RemoveRange(stats.Where(s => s.Mode > 3));
        
        await db.Players
            .Where(p => (byte)p.PreferredMode > 3)
            .ExecuteUpdateAsync(p =>p.SetProperty(v => v.PreferredMode, p => (GameMode)((byte)p.PreferredMode % 4)));
        
        await db.PlayerHistories.Where(h => h.Mode > 3).ExecuteDeleteAsync();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        db.ChangeTracker.Clear();
    }

    internal static StatsDto[] MergeStatistics(
        IReadOnlyCollection<StatsDto> stats
    ) {
        return stats.GroupBy(s => (s.PlayerId, Mode: (byte)(s.Mode % 4))).Select(group =>
        {
            var combined = group.FirstOrDefault(s => s.Mode == group.Key.Mode)
                ?? new StatsDto { PlayerId = group.Key.PlayerId, Mode = group.Key.Mode };
            
            foreach (var extra in group.Where(s => s.Mode > 3))
            {
                combined.TotalScore += extra.TotalScore;
                combined.PlayCount += extra.PlayCount;
                combined.PlayTime += extra.PlayTime;
                combined.ReplayViews += extra.ReplayViews;
                combined.TotalGekis += extra.TotalGekis;
                combined.TotalKatus += extra.TotalKatus;
                combined.Total300s += extra.Total300s;
                combined.Total100s += extra.Total100s;
                combined.Total50s += extra.Total50s;
                combined.MaxCombo = Math.Max(combined.MaxCombo, extra.MaxCombo);
            }
            return combined;
        }).ToArray();
    }

    internal static void MigrateStatistics(ScoreDto score)
    {
        if (score.LazerMods != null)
            return;

        var stats = score.Statistics;
        var canonical = score.Mode switch
        {
            GameMode.Mania => stats.ContainsKey(HitResult.Perfect),
            GameMode.Catch => stats.ContainsKey(HitResult.SmallTickHit),
            GameMode.Taiko => stats.ContainsKey(HitResult.LargeBonus),
            _ => !stats.ContainsKey(HitResult.LargeTickHit) && !stats.ContainsKey(HitResult.SliderTailHit)
        };
        if (canonical) return;

        score.Statistics = new Score
        {
            Mode = score.Mode,
            Count300 = stats.GetValueOrDefault(HitResult.Great),
            Count100 = stats.GetValueOrDefault(HitResult.Ok),
            Count50 = stats.GetValueOrDefault(HitResult.Meh),
            Gekis = stats.GetValueOrDefault(HitResult.LargeTickHit),
            Katus = stats.GetValueOrDefault(HitResult.SliderTailHit),
            Misses = stats.GetValueOrDefault(HitResult.Miss)
        }.GetStatistics();
    }

    internal static void StandardiseMap(
        IReadOnlyCollection<ScoreDto> scores,
        BeatmapStatus mapStatus,
        string checksum,
        WorkingBeatmap? working
    ) {
        foreach (var score in scores)
            score.Mode = (GameMode)((byte)score.Mode % 4);

        var visibleGroups = scores
            .Where(s => s.BeatmapMD5 == checksum)
            .Where(s => s.Status >= SubmissionStatus.BestWithMods || s.TotalScoreStatus >= SubmissionStatus.BestWithMods)
            .Select(s => (s.PlayerId, s.Mode))
            .ToHashSet();
        
        foreach (var score in scores)
        {
            if (score.LazerMods != null)
            {
                if (score.TotalScore == 0)
                    score.TotalScore = score.LegacyTotalScore;
                score.LegacyTotalScore = 0;
            }
            
            MigrateStatistics(score);
            
            score.Ranked = score.Passed
                           && mapStatus is BeatmapStatus.Ranked or BeatmapStatus.Approved 
                           && (score.Player.Privileges & 1) == 1;
            
            if (working != null)
                ScoreStandardisation.Populate(score, working);
            else
                score.MissCount = score.Statistics.GetValueOrDefault(HitResult.Miss);
            
            score.Processed = true;
            score.TotalScoreStatus = score.Status == SubmissionStatus.Failed ? SubmissionStatus.Failed : SubmissionStatus.Submitted;
            score.ModKeys = string.Join(';', score.ModKeys.Split(';', StringSplitOptions.RemoveEmptyEntries).Order()) + (score.ModKeys.Length > 0 ? ";" : "");
        }

        if (working == null)
        {
            foreach (var score in scores.Where(s => s.Status != SubmissionStatus.Failed))
                score.Status = score.TotalScoreStatus = SubmissionStatus.Submitted;
            
            return;
        }
        
        // Preserve deliberately hidden groups.
        foreach (var group in scores.Where(s => s.BeatmapMD5 == checksum).GroupBy(s => (s.PlayerId, s.Mode)).Where(g => visibleGroups.Contains(g.Key)))
            ScoreBestSelection.Assign(group.ToArray());
    }
}