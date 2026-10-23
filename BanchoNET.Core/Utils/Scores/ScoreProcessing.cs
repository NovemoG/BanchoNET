using System.Linq.Expressions;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Utils.Scores;

public static class ScoreProcessing
{
    public static Expression<Func<ScoreDto, bool>> MatchSubmission(
        ScoreDto score
    ) {
        return score.LazerMods == null
            ? existing => existing.LazerMods == null && existing.PlayerId == score.PlayerId && existing.OnlineChecksum == score.OnlineChecksum
            : existing => existing.Id == score.Id;
    }

    public static async Task LockPlayer(
        BanchoDbContext db,
        int playerId,
        GameMode mode
    ) {
        var key = -((long)playerId << 8 | (byte)mode) - 1;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})");
    }

    public static void AddStatistics(
        StatsDto stats,
        ScoreDto score,
        int? playTimeSeconds
    ) {
        stats.PlayCount++;
        stats.TotalScore += score.TotalScore;
        
        RecordDuration(stats, score, playTimeSeconds);
        
        var hits = score.Statistics;
        var mode = score.Mode;
        
        stats.Total300s += hits.GetValueOrDefault(HitResult.Great);
        stats.Total100s += hits.GetValueOrDefault(mode == GameMode.Catch ? HitResult.LargeTickHit : HitResult.Ok);
        stats.Total50s += hits.GetValueOrDefault(mode == GameMode.Catch ? HitResult.SmallTickHit : HitResult.Meh);
        
        if (mode is GameMode.Mania or GameMode.Taiko)
        {
            stats.TotalGekis += hits.GetValueOrDefault(mode == GameMode.Mania ? HitResult.Perfect : HitResult.LargeBonus);
            stats.TotalKatus += hits.GetValueOrDefault(mode == GameMode.Mania ? HitResult.Good : HitResult.SliderTailHit);
        }
        
        if (score is { Passed: true, Ranked: true })
            stats.MaxCombo = Math.Max(stats.MaxCombo, score.MaxCombo);
    }

    public static bool RecordDuration(
        StatsDto stats,
        ScoreDto score,
        int? seconds
    ) {
        if (score.PlayTimeRecorded || seconds == null)
            return false;
        
        var duration = Math.Max(0, seconds.Value);
        
        stats.PlayTime += duration;
        score.PlayTimeRecorded = true;
        
        if (score.LazerMods != null)
            score.TimeElapsed = duration;
        
        return true;
    }

    public static int LegacyPlayTimeSeconds(int milliseconds, double clockRate)
        => (int)Math.Floor(Math.Max(0, milliseconds) / (1000 * clockRate));

    public static int LazerPlayTimeSeconds(double frameTimeMilliseconds, int leadInSeconds, double clockRate)
        => (int)Math.Floor(Math.Max(0, frameTimeMilliseconds / 1000 - leadInSeconds) / clockRate);
}