using System.Text.Json;
using BanchoNET.Core.Dependencies.Official;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;
using OsuLegacyMods = osu.Game.Beatmaps.Legacy.LegacyMods;

namespace BanchoNET.Core.Utils.Scores;

public static class ScoreStandardisation
{
    private static readonly Ruleset[] Rulesets = [new OsuRuleset(), new TaikoRuleset(), new CatchRuleset(), new ManiaRuleset()];

    public static Ruleset RulesetFor(int ruleset) => ruleset is >= 0 and <= 3
        ? Rulesets[ruleset]
        : throw new ArgumentOutOfRangeException(nameof(ruleset));

    public static void Populate(
        ScoreDto score
    ) {
        var beatmap = BanchoWorkingBeatmap.FromFile(Storage.GetBeatmapPath(score.MapId), score.MapId);
        Populate(score, beatmap);
    }

    public static void Populate(
        ScoreDto score,
        WorkingBeatmap beatmap
    ) {
        var ruleset = RulesetFor((int)score.Mode);
        var legacy = score.LazerMods == null;
        
        Mod[] mods;
        
        if (legacy)
        {
            var converted = ruleset.ConvertFromLegacyMods((OsuLegacyMods)(int)score.Mods).ToList();
            
            if (ruleset.CreateMod<ModClassic>() is { } classic)
                converted.Add(classic);
            
            mods = converted.ToArray();
        }
        else
        {
            mods = score.LazerMods!.ToMods().Select(m => new APIMod
            {
                Acronym = m.Acronym,
                Settings = m.Settings.ToDictionary(k => k.Key,
                    k => k.Value is JsonElement e
                        ? e.ValueKind switch {
                            JsonValueKind.Number => e.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => e.GetString()!,
                            _ => throw new InvalidOperationException("Unsupported mod setting")
                        }
                        : k.Value)
            }.ToMod(ruleset)).ToArray();
        }

        var info = new ScoreInfo
        {
            Ruleset = ruleset.RulesetInfo,
            BeatmapInfo = beatmap.BeatmapInfo,
            Mods = mods,
            Accuracy = score.Acc / 100d,
            MaxCombo = score.MaxCombo,
            TotalScore = score.TotalScore,
            TotalScoreWithoutMods = score.TotalScoreWithoutMods,
            LegacyTotalScore = legacy ? score.LegacyTotalScore : null,
            IsLegacyScore = legacy,
            Statistics = score.Statistics.ToDictionary(s => (osu.Game.Rulesets.Scoring.HitResult)(int)s.Key, s => s.Value)
        };
        
        if (legacy)
            StandardisedScoreMigrationTools.UpdateFromLegacy(info, beatmap);
        else if (info is { TotalScoreWithoutMods: 0, TotalScore: > 0 })
            LegacyScoreDecoder.PopulateTotalScoreWithoutMods(info);

        score.TotalScore = info.TotalScore;
        score.TotalScoreWithoutMods = info.TotalScoreWithoutMods;
        score.MissCount = score.Statistics.GetValueOrDefault(Models.Scores.HitResult.Miss);
    }
}