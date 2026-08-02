#if OFFICIAL_PP
using System.Text.Json;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using BanchoMod = BanchoNET.Core.Models.Mods.Mod;
using BanchoLegacyMods = BanchoNET.Core.Models.Mods.LegacyMods;
using OsuLegacyMods = osu.Game.Beatmaps.Legacy.LegacyMods;
using OsuHitResult = osu.Game.Rulesets.Scoring.HitResult;
using BanchoHitResult = BanchoNET.Core.Models.Scores.HitResult;

namespace BanchoNET.Core.Dependencies.Official;

internal static class OfficialCalculator
{
    public const double DefaultSectionLength = 400d;

    private static readonly Ruleset[] Rulesets;

    static OfficialCalculator()
    {
        osu.Framework.Logging.Logger.Enabled = false;

        Rulesets =
        [
            new OsuRuleset(),
            new TaikoRuleset(),
            new CatchRuleset(),
            new ManiaRuleset()
        ];
    }

    public static Ruleset RulesetFor(
        int rulesetId
    ) {
        return rulesetId is >= 0 and <= 3
            ? Rulesets[rulesetId]
            : Rulesets[0];
    }

    public static Ruleset RulesetFor(
        GameMode mode
    ) => RulesetFor((int)mode.AsVanilla());

    public static BanchoWorkingBeatmap? LoadBeatmap(
        int beatmapId
    ) {
        var path = Storage.GetBeatmapPath(beatmapId);

        if (!File.Exists(path))
        {
            Logger.Shared.LogWarning($"Beatmap file not found: {path}", caller: nameof(OfficialCalculator));
            return null;
        }

        return BanchoWorkingBeatmap.FromFile(path, beatmapId);
    }

    public static Mod[] LegacyMods(
        Ruleset ruleset,
        BanchoLegacyMods mods
    ) {
        var converted = ruleset.ConvertFromLegacyMods((OsuLegacyMods)(int)mods).ToList();
        var classic = ruleset.CreateMod<ModClassic>();

        if (classic != null)
            converted.Add(classic);

        return converted.ToArray();
    }

    public static Mod[] LazerMods(
        Ruleset ruleset,
        IEnumerable<BanchoMod> mods
    ) {
        var converted = new List<Mod>();

        foreach (var mod in mods)
        {
            var apiMod = new APIMod
            {
                Acronym = mod.Acronym,
                Settings = NormalizeSettings(mod.Settings)
            };

            var result = apiMod.ToMod(ruleset);

            if (result is UnknownMod)
            {
                Logger.Shared.LogWarning(
                    $"Unknown mod acronym '{mod.Acronym}' for ruleset {ruleset.ShortName}",
                    caller: nameof(OfficialCalculator));

                continue;
            }

            if (result is ModTimeRamp ramp)
                ramp.InitialRate.Value = Math.Min(ramp.InitialRate.Value, ramp.FinalRate.Value);

            converted.Add(result);
        }

        return converted.ToArray();
    }

    public static ScoreInfo LegacyScore(
        Ruleset ruleset,
        BanchoWorkingBeatmap beatmap,
        Mod[] mods,
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
        var score = new ScoreInfo
        {
            Ruleset = ruleset.RulesetInfo,
            BeatmapInfo = beatmap.BeatmapInfo,
            Mods = mods,
            Accuracy = accuracy / 100d,
            MaxCombo = maxCombo,
            TotalScore = totalScore,
            LegacyTotalScore = totalScore,
            IsLegacyScore = true
        };

        score.SetCount300(count300);
        score.SetCount100(count100);
        score.SetCount50(count50);
        score.SetCountGeki(gekis);
        score.SetCountKatu(katus);
        score.SetCountMiss(misses);

        return score;
    }

    public static ScoreInfo LazerScore(
        Ruleset ruleset,
        BanchoWorkingBeatmap beatmap,
        Mod[] mods,
        double accuracy,
        int maxCombo,
        long totalScore,
        long? legacyTotalScore,
        Dictionary<BanchoHitResult, int> statistics
    ) {
        return new ScoreInfo
        {
            Ruleset = ruleset.RulesetInfo,
            BeatmapInfo = beatmap.BeatmapInfo,
            Mods = mods,
            Accuracy = accuracy,
            MaxCombo = maxCombo,
            TotalScore = totalScore,
            LegacyTotalScore = legacyTotalScore,
            IsLegacyScore = legacyTotalScore.HasValue,
            Statistics = statistics.ToDictionary(s => (OsuHitResult)(int)s.Key, s => s.Value)
        };
    }

    public static float Performance(
        Ruleset ruleset,
        BanchoWorkingBeatmap beatmap,
        ScoreInfo score
    ) {
        var attributes = ruleset.CreateDifficultyCalculator(beatmap).Calculate(score.Mods);
        var calculator = ruleset.CreatePerformanceCalculator();

        if (calculator == null)
            return 0f;

        var pp = calculator.Calculate(score, attributes).Total;

        return double.IsFinite(pp) ? (float)pp : 0f;
    }

    public static DifficultyGraph Graph(
        BanchoWorkingBeatmap beatmap,
        Mod[] mods,
        double sectionLength
    ) {
        var ruleset = RulesetFor(beatmap.BeatmapInfo.Ruleset.OnlineID);
        var attributes = ruleset.CreateDifficultyCalculator(beatmap).Calculate(mods);
        var timed = ruleset.CreateDifficultyCalculator(beatmap).CalculateTimed(mods);

        if (timed.Count == 0)
        {
            return new DifficultyGraph
            {
                SectionLength = sectionLength,
                StarRating = attributes.StarRating,
                MaxCombo = attributes.MaxCombo
            };
        }

        var sectionCount = (int)Math.Ceiling(timed[^1].Time / sectionLength) + 1;
        var sections = new List<DifficultyGraphSection>(sectionCount);

        var index = 0;
        var carried = 0d;
        var maxValue = 0d;

        for (var section = 0; section < sectionCount; section++)
        {
            var start = section * sectionLength;
            var end = start + sectionLength;

            while (index < timed.Count && timed[index].Time < end)
            {
                carried = Math.Max(carried, timed[index].Attributes.StarRating);
                index++;
            }

            maxValue = Math.Max(maxValue, carried);

            sections.Add(new DifficultyGraphSection
            {
                StartTime = start,
                EndTime = end,
                StarRating = carried
            });
        }

        return new DifficultyGraph
        {
            SectionLength = sectionLength,
            TotalLength = sectionCount * sectionLength,
            MaxValue = maxValue,
            StarRating = attributes.StarRating,
            MaxCombo = attributes.MaxCombo,
            Sections = sections
        };
    }

    private static Dictionary<string, object> NormalizeSettings(
        Dictionary<string, object> settings
    ) {
        var normalized = new Dictionary<string, object>(settings.Count);

        foreach (var (key, value) in settings)
        {
            var setting = value switch
            {
                JsonElement { ValueKind: JsonValueKind.Number } element => element.GetDouble(),
                JsonElement { ValueKind: JsonValueKind.True } => true,
                JsonElement { ValueKind: JsonValueKind.False } => false,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                JsonElement => null,
                _ => value
            };

            if (setting != null)
                normalized[key] = setting;
        }

        return normalized;
    }
}
#endif