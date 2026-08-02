#if OFFICIAL_PP
using System.Text.Json;
using BanchoNET.Core.Dependencies.Official;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using BanchoMod = BanchoNET.Core.Models.Mods.Mod;

namespace Pp;

public static partial class PpMethods
{
    private static readonly JsonSerializerOptions GraphSerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public static float ComputeScorePp(
        Beatmap beatmap,
        Score score
    ) {
        return LegacyPerformance(
            beatmap.Id,
            score.Mode,
            score.Mods,
            score.Acc,
            score.MaxCombo,
            score.TotalScore,
            score.Count300,
            score.Count100,
            score.Count50,
            score.Gekis,
            score.Katus,
            score.Misses
        );
    }

    public static float ComputeScorePp(
        int beatmapId,
        ApiScore score
    ) {
        return Guarded(() =>
        {
            var ruleset = OfficialCalculator.RulesetFor(score.RulesetId);
            var beatmap = OfficialCalculator.LoadBeatmap(beatmapId);

            if (beatmap == null) return 0f;

            var mods = OfficialCalculator.LazerMods(ruleset, score.Mods);

            var scoreInfo = OfficialCalculator.LazerScore(
                ruleset,
                beatmap,
                mods,
                score.Accuracy / 100d,
                score.MaxCombo,
                score.TotalScore,
                score.LegacyTotalScore,
                score.Statistics
            );

            return OfficialCalculator.Performance(ruleset, beatmap, scoreInfo);
        });
    }

    public static float ComputeScorePp(
        int beatmapId,
        ScoreDto score,
        BanchoMod[] mods
    ) {
        if (score.LazerMods == null)
        {
            return LegacyPerformance(
                beatmapId,
                score.Mode,
                score.Mods,
                score.Acc,
                score.MaxCombo,
                score.LegacyTotalScore,
                score.GetCount300(),
                score.GetCount100(),
                score.GetCount50(),
                score.GetCountGeki(),
                score.GetCountKatu(),
                score.GetCountMiss()
            );
        }

        return Guarded(() =>
        {
            var ruleset = OfficialCalculator.RulesetFor(score.Mode);
            var beatmap = OfficialCalculator.LoadBeatmap(beatmapId);

            if (beatmap == null) return 0f;

            var scoreInfo = OfficialCalculator.LazerScore(
                ruleset,
                beatmap,
                OfficialCalculator.LazerMods(ruleset, mods),
                score.Acc / 100d,
                score.MaxCombo,
                score.TotalScore,
                score.LegacyTotalScore > 0 ? score.LegacyTotalScore : null,
                score.Statistics
            );

            return OfficialCalculator.Performance(ruleset, beatmap, scoreInfo);
        });
    }

    public static float ComputeNoMissesScorePp(
        Beatmap beatmap,
        Score score,
        int maxCombo
    ) {
        var acc = ScoreExtensions.CalculateAccuracy(
            score.Mode,
            score.Mods,
            score.Count300 + score.Misses,
            score.Count100,
            score.Count50,
            0,
            score.Gekis,
            score.Katus
        );

        return LegacyPerformance(
            beatmap.Id,
            score.Mode,
            score.Mods,
            acc,
            maxCombo,
            score.TotalScore,
            score.Count300 + score.Misses,
            score.Count100,
            score.Count50,
            score.Gekis,
            score.Katus,
            0
        );
    }

    public static float ComputeNoMissesScorePp(
        Beatmap beatmap,
        ScoreDto score,
        int maxCombo
    ) {
        var acc = ScoreExtensions.CalculateAccuracy(
            score.Mode,
            score.Mods,
            score.GetCount300() + score.GetCountMiss(),
            score.GetCount100(),
            score.GetCount50(),
            0,
            score.GetCountGeki(),
            score.GetCountKatu()
        );

        return LegacyPerformance(
            beatmap.Id,
            score.Mode,
            score.Mods,
            acc,
            maxCombo,
            score.LegacyTotalScore,
            score.GetCount300() + score.GetCountMiss(),
            score.GetCount100(),
            score.GetCount50(),
            score.GetCountGeki(),
            score.GetCountKatu(),
            0
        );
    }

    public static string CalculateGraphJson(
        int beatmapId,
        uint mods = 0
    ) {
        return JsonSerializer.Serialize(
            CalculateGraph(beatmapId, (BanchoNET.Core.Models.Mods.LegacyMods)mods),
            GraphSerializerOptions
        );
    }

    public static DifficultyGraph CalculateGraph(
        int beatmapId,
        BanchoNET.Core.Models.Mods.LegacyMods mods = BanchoNET.Core.Models.Mods.LegacyMods.None,
        double sectionLength = OfficialCalculator.DefaultSectionLength
    ) {
        var beatmap = OfficialCalculator.LoadBeatmap(beatmapId)
                      ?? throw new FileNotFoundException($"Beatmap {beatmapId} is not available locally");

        var ruleset = OfficialCalculator.RulesetFor(beatmap.BeatmapInfo.Ruleset.OnlineID);

        return OfficialCalculator.Graph(
            beatmap,
            OfficialCalculator.LegacyMods(ruleset, mods),
            sectionLength
        );
    }

    private static float LegacyPerformance(
        int beatmapId,
        GameMode mode,
        BanchoNET.Core.Models.Mods.LegacyMods mods,
        float accuracy,
        int maxCombo,
        long totalScore,
        int count300,
        int count100,
        int count50,
        int gekis,
        int katus,
        int misses
    ) {
        return Guarded(() =>
        {
            var ruleset = OfficialCalculator.RulesetFor(mode);
            var beatmap = OfficialCalculator.LoadBeatmap(beatmapId);

            if (beatmap == null) return 0f;

            var scoreInfo = OfficialCalculator.LegacyScore(
                ruleset,
                beatmap,
                OfficialCalculator.LegacyMods(ruleset, mods),
                accuracy,
                maxCombo,
                totalScore,
                count300,
                count100,
                count50,
                gekis,
                katus,
                misses
            );

            return OfficialCalculator.Performance(ruleset, beatmap, scoreInfo);
        });
    }

    private static float Guarded(
        Func<float> calculation
    ) {
        try
        {
            return calculation();
        }
        catch (Exception exception)
        {
            Logger.Shared.LogError("Performance calculation failed", exception, caller: nameof(PpMethods));
            return 0f;
        }
    }
}
#endif