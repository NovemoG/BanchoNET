using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Scores;
using osu.Game.Scoring;
using Pp;
using osu.Game.Scoring.Legacy;
using Score = BanchoNET.Core.Models.Scores.Score;

namespace BanchoNET.Core.Utils.Extensions;

public static class ScoreExtensions
{
    public static string ComputeOnlineChecksum(this Score score,
        string osuVersion,
        string clientHash,
        string storyboardChecksum
    ) {
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
            score.Mode,
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

        switch (mode)
        {
            case GameMode.Standard:
                objects = count300 + count100 + count50 + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count300 * 300f + count100 * 100f + count50 * 50f) / (objects * 300f);
                break;
            case GameMode.Taiko:
                objects = count300 + count100 + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count100 * 0.5f + count300) / objects;
                break;
            case GameMode.Catch:
                objects = count300 + count100 + count50 + katus + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count300 + count100 + count50) / objects;
                break;
            case GameMode.Mania:
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

        var lazer = score.LazerMods != null && mods.FirstOrDefault(m => m.Acronym == "CL") == null;
        var da = mods.FirstOrDefault(m => m.Acronym == "DA"); // Difficulty Adjust

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

        var clockRate = mods.GetClockRate();

        var pp = PpMethods.ComputeScorePp(beatmap.Id, score, clockRate, lazer, cs, ar, od);
#endif

        Logger.Shared.LogInfo($"Recalculated score {score.Id}: {score.PP} -> {pp}", nameof(ScoreExtensions));
        
        score.PP = double.IsInfinity(pp) || double.IsNaN(pp) ? 0.0f : MathF.Round(pp, 5);
    }

    public static string ModeToString(this Score score)
    {
        return score.Mode switch
        {
            GameMode.Standard => "std",
            GameMode.Taiko => "taiko",
            GameMode.Catch => "catch",
            GameMode.Mania => "mania",
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
        var info = new ScoreInfo
        {
            Ruleset = ScoreStandardisation.RulesetFor((int)score.Mode).RulesetInfo
        };
        
        info.SetCount300(score.Count300);
        info.SetCount100(score.Count100);
        info.SetCount50(score.Count50);
        info.SetCountGeki(score.Gekis);
        info.SetCountKatu(score.Katus);
        info.SetCountMiss(score.Misses);
        
        return info.Statistics.ToDictionary(s => (HitResult)(int)s.Key, s => s.Value);
    }
    
    extension(
        ApiScore score
    ) {
        public int GetCount300() => score.Statistics.GetStatCount(HitResult.Great);
        public int GetCount100() => score.Statistics.GetStatCount((GameMode)score.RulesetId == GameMode.Catch ? HitResult.LargeTickHit : HitResult.Ok);
        public int GetCount50() => score.Statistics.GetStatCount((GameMode)score.RulesetId == GameMode.Catch ? HitResult.SmallTickHit : HitResult.Meh);
        public int GetCountGeki()
        {
            return score.Statistics.GetStatCount((GameMode)score.RulesetId switch
            {
                GameMode.Mania => HitResult.Perfect,
                GameMode.Taiko => HitResult.LargeBonus,
                _ => HitResult.LargeTickHit
            });
        }
        public int GetCountKatu()
        {
            return score.Statistics.GetStatCount((GameMode)score.RulesetId switch
            {
                GameMode.Mania => HitResult.Good,
                GameMode.Catch => HitResult.SmallTickMiss,
                _ => HitResult.SliderTailHit
            });
        }
        public int GetCountMiss() => score.Statistics.GetStatCount(HitResult.Miss);
        public int GetCountIgnoreHit() => score.Statistics.GetStatCount(HitResult.IgnoreHit);
        public int GetCountIgnoreMiss() => score.Statistics.GetStatCount(HitResult.IgnoreMiss);
    }

    extension(
        ScoreDto score
    ) {
        public int GetCount300() => score.Statistics.GetStatCount(HitResult.Great);
        public int GetCount100()
        {
            return score.Statistics.GetStatCount(score.Mode == GameMode.Catch ? HitResult.LargeTickHit : HitResult.Ok);
        }
        public int GetCount50()
        {
            return score.Statistics.GetStatCount(score.Mode == GameMode.Catch ? HitResult.SmallTickHit : HitResult.Meh);
        }
        public int GetCountGeki()
        {
            return score.Statistics.GetStatCount(score.Mode switch
            {
                GameMode.Mania => HitResult.Perfect,
                GameMode.Taiko => HitResult.LargeBonus,
                _ => HitResult.LargeTickHit
            });
        }
        public int GetCountKatu()
        {
            return score.Statistics.GetStatCount(score.Mode switch
            {
                GameMode.Mania => HitResult.Good,
                GameMode.Catch => HitResult.SmallTickMiss,
                _ => HitResult.SliderTailHit
            });
        }
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