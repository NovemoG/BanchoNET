using BanchoNET.Core.Dependencies.Official;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Scores;

namespace BanchoNET.Tests;

public class ScoreStandardisationTests
{
    [TestCase(GameMode.Standard)]
    [TestCase(GameMode.Taiko)]
    [TestCase(GameMode.Catch)]
    [TestCase(GameMode.Mania)]
    public void StableConversionPreservesRawTotalAndProducesStandardisedTotal(GameMode mode)
    {
        WithBeatmap((int)mode, beatmap =>
        {
            var score = Score(mode);
            score.LegacyTotalScore = 300;
            score.LazerMods = null;
            ScoreStandardisation.Populate(score, beatmap);
            Assert.That(score.LegacyTotalScore, Is.EqualTo(300));
            Assert.That(score.TotalScore, Is.GreaterThan(0));
            Assert.That(score.TotalScoreWithoutMods, Is.GreaterThan(0));
        });
    }

    [Test]
    public void LazerPreservesSubmittedTotalsAndReconstructsMissingNoModTotal()
    {
        WithBeatmap(0, beatmap =>
        {
            var score = Score(GameMode.Standard);
            score.LazerMods = "";
            score.TotalScore = 912345;
            score.TotalScoreWithoutMods = 0;
            ScoreStandardisation.Populate(score, beatmap);
            Assert.That(score.TotalScoreWithoutMods, Is.EqualTo(912345));
            score.TotalScoreWithoutMods = 123456;
            ScoreStandardisation.Populate(score, beatmap);
            Assert.That(score.TotalScore, Is.EqualTo(912345));
            Assert.That(score.TotalScoreWithoutMods, Is.EqualTo(123456));
        });
    }

    [Test]
    public void StoredCustomSpeedSettingsCanBeStandardisedWithoutChangingSubmittedTotal()
    {
        WithBeatmap(0, beatmap =>
        {
            var score = Score(GameMode.Standard);
            score.LazerMods = "DT,speed_change=1.2;";
            score.TotalScore = 900000;
            ScoreStandardisation.Populate(score, beatmap);
            Assert.That(score.TotalScore, Is.EqualTo(900000));
            Assert.That(score.TotalScoreWithoutMods, Is.GreaterThan(0));
        });
    }

    private static ScoreDto Score(GameMode mode) => new()
    {
        BeatmapMD5 = "", MapId = 1, ModKeys = "", Mode = mode, Acc = 100, MaxCombo = 1,
        Status = SubmissionStatus.Submitted,
        Statistics = new() { [mode == GameMode.Mania ? HitResult.Perfect : HitResult.Great] = 1 }
    };

    private static void WithBeatmap(int mode, Action<osu.Game.Beatmaps.WorkingBeatmap> action)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, $"""
                osu file format v14

                [General]
                AudioFilename: audio.mp3
                Mode: {mode}

                [Metadata]
                Title:Test
                Artist:Test
                Creator:Test
                Version:Test

                [Difficulty]
                HPDrainRate:5
                CircleSize:4
                OverallDifficulty:5
                ApproachRate:5
                SliderMultiplier:1.4
                SliderTickRate:1

                [TimingPoints]
                0,500,4,2,1,100,1,0

                [HitObjects]
                256,192,1000,1,0,0:0:0:0:
                """);
            action(BanchoWorkingBeatmap.FromFile(path));
        }
        finally { File.Delete(path); }
    }
}
