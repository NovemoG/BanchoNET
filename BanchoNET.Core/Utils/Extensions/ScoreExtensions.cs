using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using Pp;
using Score = BanchoNET.Core.Models.Scores.Score;

namespace BanchoNET.Core.Utils.Extensions;

public static class ScoreExtensions
{
    public static string ComputeOnlineChecksum(this Score score,
        string osuVersion,
        string clientHash,
        string storyboardChecksum)
    {
        if (score.Player == null || score.Beatmap == null) return "";

        return string.Format(
            "chickenmcnuggets{0}o15{1}{2}smustard{3}{4}uu{5}{6}{7}{8}{9}{10}{11}Q{12}{13}{15}{14:yyMMddHHmmss}{16}{17}",
            score.Count300 + score.Count100,
            score.Count50,
            score.Gekis,
            score.Katus,
            score.Misses,
            score.Beatmap.Checksum,
            score.MaxCombo,
            score.Perfect,
            score.Player.Username,
            score.TotalScore,
            score.Grade.ToString(),
            (int)score.Mods,
            score.Passed,
            (int)score.Mode.AsVanilla(),
            score.ClientTime,
            osuVersion,
            clientHash,
            storyboardChecksum).CreateMD5();
    }

    public static void CalculateAccuracy(this Score score)
    {
        score.Acc = CalculateAccuracy(
            score.Mode,
            score.Mods,
            score.Count300,
            score.Count100,
            score.Count50,
            score.Misses,
            score.Gekis,
            score.Katus);
    }

    public static float CalculateAccuracy(
        GameMode mode,
        LegacyMods mods,
        int count300,
        int count100,
        int count50,
        int misses,
        int gekis,
        int katus
    ) {
        int objects;
        float acc;

        switch (mode.AsVanilla())
        {
            case GameMode.VanillaStd:
                objects = count300 + count100 + count50 + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count300 * 300f + count100 * 100f + count50 * 50f) / (objects * 300f);
                break;
            case GameMode.VanillaTaiko:
                objects = count300 + count100 + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count100 * 0.5f + count300) / objects;
                break;
            case GameMode.VanillaCatch:
                objects = count300 + count100 + count50 + katus + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count300 + count100 + count50) / objects;
                break;
            case GameMode.VanillaMania:
                objects = count300 + count100 + count50 + gekis + katus + misses;

                if (objects == 0) return 0f;

                if ((mods & LegacyMods.ScoreV2) == LegacyMods.ScoreV2)
                    return 100f
                           * (gekis * 305f + count300 * 300f + katus * 200f + count100 * 100f + count50 * 50f)
                           / (objects * 305f);

                acc = 100f
                      * ((count300 + gekis) * 300f + katus * 200f + count100 * 100f + count50 * 50f)
                      / (objects * 300f);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), $"Invalid mode {mode}");
        }

        return acc;
    }

    public static void IncreasePlaytime(
        this StatsDto stats,
        LegacyMods mods,
        int timeElapsed
    ) {
        if (mods.HasMod(LegacyMods.DoubleTime) || mods.HasMod(LegacyMods.NightCore))
        {
            stats.PlayTime += (int)MathF.Floor(timeElapsed / 1500f);
        }
        else if (mods.HasMod(LegacyMods.HalfTime))
        {
            stats.PlayTime += (int)MathF.Floor(timeElapsed / 750f);
        }
        else
        {
            stats.PlayTime += (int)MathF.Floor(timeElapsed / 1000f);
        }
    }

    public static void IncreasePlaytime(
        this StatsDto stats,
        double clockRate,
        int timeElapsed
    ) {
        stats.PlayTime += (int)Math.Floor(timeElapsed * clockRate);
    }

    public static float CalculateCompletion(this Score score, Beatmap beatmap)
    {
        return (float)(score.Count300 + score.Count100 + score.Count50 + score.Misses)
            / (beatmap.CirclesCount + beatmap.SlidersCount + beatmap.SpinnersCount) * 100f;
    }

    public static bool EqualModsWith(
        this ApiScore? score,
        ApiScore? other
    ) {
        if (ReferenceEquals(score, other))
            return true;

        if (score is null || other is null)
            return false;

        return score.ModKeys == other.ModKeys;
    }

    public static bool EqualModsWith(
        this Score? score,
        Score? other
    ) {
        if (ReferenceEquals(score, other))
            return true;

        if (score is null || other is null)
            return false;

        return score.ModKeys == other.ModKeys;
    }

    public static void CalculatePerformance(
        this Score score,
        Beatmap beatmap
    ) {
        if (score.Mods.HasMod(LegacyMods.NightCore))
            score.Mods |= LegacyMods.DoubleTime;

        var pp = PpMethods.ComputeScorePp(beatmap, score);

        score.PP = double.IsInfinity(pp) || double.IsNaN(pp) ? 0.0f : MathF.Round(pp, 5);

        Logger.Shared.LogInfo($"Submitted legacy score pp: {score.PP}", nameof(ScoreExtensions));
    }

    public static void CalculatePerformance(
        this ApiScore score,
        Beatmap beatmap
    ) {
#if OFFICIAL_PP
        var pp = PpMethods.ComputeScorePp(beatmap.Id, score);
#else
        var mods = score.Mods;

        if (mods.FirstOrDefault(m => m.Acronym == "RD") != null)
        {
            score.Pp = 0;
            return;
        }
        
        var lazer = mods.FirstOrDefault(m => m.Acronym == "CL") == null;
        var da = mods.FirstOrDefault(m => m.Acronym == "DA"); // Difficulty Adjust

        var cs = beatmap.Cs;
        var ar = beatmap.Ar;
        var od = beatmap.Od;
        if (da != null)
        {
            if (da.Settings.TryGetValue("circle_size", out var circleSize))
                cs = circleSize.GetFloat();

            if (da.Settings.TryGetValue("approach_rate", out var approachRate))
                ar = approachRate.GetFloat();

            if (da.Settings.TryGetValue("overall_difficulty", out var overallDifficulty))
                od = overallDifficulty.GetFloat();
        }

        var pp = PpMethods.ComputeScorePp(beatmap.Id, score, score.ClockRate, lazer, cs, ar, od);
#endif

        score.Pp = double.IsInfinity(pp) || double.IsNaN(pp) ? 0.0f : MathF.Round(pp, 5);

        Logger.Shared.LogInfo($"Submitted lazer score pp: {score.Pp}", nameof(ScoreExtensions));
    }

    public static void RecalculatePerformance(
        this ScoreDto score,
        BeatmapDto beatmap
    ) {
        var mods = score.LazerMods?.ToMods() ?? score.ModKeys.ToMods();
        
#if OFFICIAL_PP
        var pp = PpMethods.ComputeScorePp(beatmap.Id, score, mods);
#else
        if (mods.FirstOrDefault(m => m.Acronym == "RD") != null)
        {
            score.PP = 0;
            return;
        }

        var lazer = mods.FirstOrDefault(m => m.Acronym == "CL") == null;
        var da = mods.FirstOrDefault(m => m.Acronym == "DA"); // Difficulty Adjust
        var dt = mods.FirstOrDefault(m => m.Acronym is "DT" or "NC");
        var ht = mods.FirstOrDefault(m => m.Acronym is "HT" or "DC");

        var cs = beatmap.Cs;
        var ar = beatmap.Ar;
        var od = beatmap.Od;
        if (da != null)
        {
            if (da.Settings.TryGetValue("circle_size", out var circleSize))
                cs = Convert.ToSingle(circleSize);

            if (da.Settings.TryGetValue("approach_rate", out var approachRate))
                ar = Convert.ToSingle(approachRate);

            if (da.Settings.TryGetValue("overall_difficulty", out var overallDifficulty))
                od = Convert.ToSingle(overallDifficulty);
        }

        var clockRate = dt != null ? 1.5d : ht != null ? 0.75d : 1d;
        if (dt != null && dt.Settings.TryGetValue("speed_change", out var rateChange)
            || ht != null && ht.Settings.TryGetValue("speed_change", out rateChange))
        {
            clockRate = Convert.ToDouble(rateChange);
        }

        var pp = PpMethods.ComputeScorePp(beatmap.Id, score, clockRate, lazer, cs, ar, od);
#endif

        Logger.Shared.LogInfo($"Recalculated score {score.Id}: {score.PP} -> {pp}", nameof(ScoreExtensions));
        
        score.PP = double.IsInfinity(pp) || double.IsNaN(pp) ? 0.0f : MathF.Round(pp, 5);
    }
    
    public static bool IsBetterThan(this Score score, Score? other)
    {
        if (other == null) return true;
        
        return AppSettings.SubmitByPP
            ? score.PP > other.PP
            : score.TotalScore > other.TotalScore;
    }
    
    public static bool IsBetterThan(this ApiScore score, ApiScore? other)
    {
        if (other == null) return true;
        
        return AppSettings.SubmitByPP
            ? score.Pp > other.Pp
            : score.TotalScore > other.TotalScore;
    }

    public static string ModeToString(this Score score)
    {
        return score.Mode switch
        {
            GameMode.VanillaStd => "vn!std",
            GameMode.VanillaTaiko => "vn!taiko",
            GameMode.VanillaCatch => "vn!catch",
            GameMode.VanillaMania => "vn!mania",
            GameMode.RelaxStd => "rx!std",
            GameMode.RelaxTaiko => "rx!taiko",
            GameMode.RelaxCatch => "rx!catch",
            GameMode.AutopilotStd => "ap!std",
            _ => throw new ArgumentOutOfRangeException($"Invalid score mode? ({score.Mode})")
        };
    }

    public static int GetStatCount(
        this Dictionary<HitResult, int> stats,
        HitResult result
    ) {
        return stats.TryGetValue(result, out var count) ? count : 0;
    }

    public static Dictionary<HitResult, int> GetStatistics(
        this Score score
    ) {
        var stats = new Dictionary<HitResult, int>();
        
        if (score.Count300 > 0) stats.Add(HitResult.Great, score.Count300);
        if (score.Count100 > 0) stats.Add(HitResult.Ok, score.Count100);
        if (score.Count50 > 0) stats.Add(HitResult.Meh, score.Count50);
        if (score.Gekis > 0) stats.Add(HitResult.LargeTickHit, score.Gekis);
        if (score.Katus > 0) stats.Add(HitResult.SliderTailHit, score.Katus);
        if (score.Misses > 0) stats.Add(HitResult.Miss, score.Misses);
        
        return stats;
    }
    
    extension(
        ApiScore score
    ) {
        public int GetCount300() => score.Statistics.GetStatCount(HitResult.Great);
        public int GetCount100() => score.Statistics.GetStatCount(HitResult.Ok);
        public int GetCount50() => score.Statistics.GetStatCount(HitResult.Meh);
        public int GetCountGeki() => score.Statistics.GetStatCount(HitResult.LargeTickHit);
        public int GetCountKatu() => score.Statistics.GetStatCount(HitResult.SliderTailHit);
        public int GetCountMiss() => score.Statistics.GetStatCount(HitResult.Miss);
        public int GetCountIgnoreHit() => score.Statistics.GetStatCount(HitResult.IgnoreHit);
        public int GetCountIgnoreMiss() => score.Statistics.GetStatCount(HitResult.IgnoreMiss);
    }

    extension(
        ScoreDto score
    ) {
        public int GetCount300() => score.Statistics.GetStatCount(HitResult.Great);
        public int GetCount100() => score.Statistics.GetStatCount(HitResult.Ok);
        public int GetCount50() => score.Statistics.GetStatCount(HitResult.Meh);
        public int GetCountGeki() => score.Statistics.GetStatCount(HitResult.LargeTickHit);
        public int GetCountKatu() => score.Statistics.GetStatCount(HitResult.SliderTailHit);
        public int GetCountMiss() => score.Statistics.GetStatCount(HitResult.Miss);
        public int GetCountIgnoreHit() => score.Statistics.GetStatCount(HitResult.IgnoreHit);
        public int GetCountIgnoreMiss() => score.Statistics.GetStatCount(HitResult.IgnoreMiss);
    }

    public static bool IsHit(this HitResult result)
    {
        switch (result)
        {
            case HitResult.None:
            case HitResult.IgnoreMiss:
            case HitResult.Miss:
            case HitResult.SmallTickMiss:
            case HitResult.LargeTickMiss:
            case HitResult.ComboBreak:
                return false;

            default:
                return true;
        }
    }
}