using BanchoNET.Core.Dependencies.Official;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Tools;
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

    [Test]
    public void ManualMigrationCanBeRerunWithoutConvertingTotalsTwice()
    {
        WithBeatmap(0, working =>
        {
            var legacy = Score(GameMode.Standard);
            legacy.Id = 1;
            legacy.Player = new PlayerDto { Privileges = 1 };
            legacy.LegacyTotalScore = 300;
            legacy.Status = SubmissionStatus.Best;
            legacy.PP = 100;
            var lazer = Score(GameMode.Standard);
            lazer.Id = 2;
            lazer.Player = legacy.Player;
            lazer.LazerMods = "";
            lazer.LegacyTotalScore = 543210;
            lazer.PP = 200;
            ScoreDto[] scores = [legacy, lazer];

            ScoreMigration.StandardiseMap(scores, BeatmapStatus.Ranked, "", working);
            long converted = legacy.TotalScore;
            long withoutMods = legacy.TotalScoreWithoutMods;
            ScoreMigration.StandardiseMap(scores, BeatmapStatus.Ranked, "", working);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(legacy.TotalScore, Is.EqualTo(converted));
                Assert.That(legacy.TotalScoreWithoutMods, Is.EqualTo(withoutMods));
                Assert.That(legacy.LegacyTotalScore, Is.EqualTo(300));
                Assert.That(lazer.TotalScore, Is.EqualTo(543210));
                Assert.That(lazer.LegacyTotalScore, Is.Zero);
                Assert.That(lazer.Status, Is.EqualTo(SubmissionStatus.Best));
                Assert.That(scores.Count(s => s.TotalScoreStatus == SubmissionStatus.Best), Is.EqualTo(1));
            }
        });
    }

    [Test]
    public void ManualMigrationPreservesHiddenGroupsAndRestrictedEligibility()
    {
        WithBeatmap(0, working =>
        {
            var hidden = Score(GameMode.Standard);
            hidden.PlayerId = 1;
            hidden.Player = new PlayerDto { Privileges = 1 };
            hidden.LegacyTotalScore = 300;
            var restricted = Score(GameMode.Standard);
            restricted.PlayerId = 2;
            restricted.Player = new PlayerDto { Privileges = 0 };
            restricted.LegacyTotalScore = 300;
            restricted.Ranked = true;
            restricted.Status = SubmissionStatus.Best;

            ScoreMigration.StandardiseMap([hidden, restricted], BeatmapStatus.Ranked, "", working);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(hidden.Status, Is.EqualTo(SubmissionStatus.Submitted));
                Assert.That(hidden.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Submitted));
                Assert.That(restricted.Ranked, Is.False);
            }
        });
    }

    [TestCase(4, GameMode.Standard, "RX;")]
    [TestCase(5, GameMode.Taiko, "RX;")]
    [TestCase(6, GameMode.Catch, "RX;")]
    [TestCase(8, GameMode.Standard, "AP;")]
    public void ManualMigrationCombinesModCategoriesAndReselectsBothBests(int oldMode, GameMode mode, string mods)
    {
        WithBeatmap((int)mode, working =>
        {
            var normal = Score(mode);
            normal.Id = 1;
            normal.PlayerId = 10;
            normal.Player = new PlayerDto { Privileges = 1 };
            normal.LazerMods = "";
            normal.TotalScore = normal.TotalScoreWithoutMods = 900000;
            normal.PP = 100;
            normal.Status = SubmissionStatus.Best;
            var assisted = Score(mode);
            assisted.Id = 2;
            assisted.PlayerId = normal.PlayerId;
            assisted.Player = normal.Player;
            assisted.Mode = (GameMode)oldMode;
            assisted.ModKeys = assisted.LazerMods = mods;
            assisted.TotalScore = assisted.TotalScoreWithoutMods = 800000;
            assisted.PP = 200;
            assisted.Status = SubmissionStatus.Best;

            ScoreMigration.StandardiseMap([normal, assisted], BeatmapStatus.Ranked, "", working);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(assisted.Mode, Is.EqualTo(mode));
                Assert.That(assisted.ModKeys, Is.EqualTo(mods));
                Assert.That(assisted.Status, Is.EqualTo(SubmissionStatus.Best));
                Assert.That(normal.Status, Is.EqualTo(SubmissionStatus.BestWithMods));
                Assert.That(normal.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Best));
                Assert.That(assisted.TotalScoreStatus, Is.EqualTo(SubmissionStatus.BestWithMods));
            }
        });
    }

    [Test]
    public void ManualMigrationKeepsOutdatedVersionScoresHidden()
    {
        WithBeatmap(0, working =>
        {
            var outdated = Score(GameMode.Standard);
            outdated.Id = 1;
            outdated.BeatmapMD5 = "old";
            outdated.Player = new PlayerDto { Privileges = 1 };
            outdated.LazerMods = "";
            outdated.TotalScore = 900000;
            outdated.PP = 300;
            var current = Score(GameMode.Standard);
            current.Id = 2;
            current.BeatmapMD5 = "new";
            current.Player = outdated.Player;
            current.LazerMods = "";
            current.TotalScore = 800000;
            current.PP = 200;
            current.Status = SubmissionStatus.Best;

            ScoreMigration.StandardiseMap([outdated, current], BeatmapStatus.Ranked, "new", working);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outdated.Status, Is.EqualTo(SubmissionStatus.Submitted));
                Assert.That(outdated.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Submitted));
                Assert.That(current.Status, Is.EqualTo(SubmissionStatus.Best));
                Assert.That(current.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Best));
            }
        });
    }

    [Test]
    public void ManualMigrationWithoutBeatmapFileHidesScoresAndKeepsTotals()
    {
        var legacy = Score(GameMode.Standard);
        legacy.Id = 1;
        legacy.Player = new PlayerDto { Privileges = 1 };
        legacy.LegacyTotalScore = 300;
        legacy.TotalScore = 123;
        legacy.Status = SubmissionStatus.Best;
        legacy.Statistics[HitResult.Miss] = 2;

        ScoreMigration.StandardiseMap([legacy], BeatmapStatus.Ranked, "", null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(legacy.TotalScore, Is.EqualTo(123));
            Assert.That(legacy.MissCount, Is.EqualTo(2));
            Assert.That(legacy.Status, Is.EqualTo(SubmissionStatus.Submitted));
            Assert.That(legacy.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Submitted));
        }
    }

    [Test]
    public void ModeMergePreservesLifetimeCountersAndCanBeRepeated()
    {
        var normal = new StatsDto
        {
            PlayerId = 1, Mode = 0, TotalScore = 100, PlayCount = 2, PlayTime = 30,
            ReplayViews = 1, MaxCombo = 20, Total300s = 10, ProfileDefaultMods = "HD;"
        };
        var relax = new StatsDto
        {
            PlayerId = 1, Mode = 4, TotalScore = 200, PlayCount = 3, PlayTime = 40,
            ReplayViews = 2, MaxCombo = 50, Total300s = 20, Total100s = 4,
            Total50s = 2, TotalGekis = 7, TotalKatus = 8
        };
        var autopilot = new StatsDto { PlayerId = 1, Mode = 8, TotalScore = 300, PlayCount = 4 };
        var taiko = new StatsDto { PlayerId = 1, Mode = 1, TotalScore = 99 };
        var otherPlayer = new StatsDto { PlayerId = 2, Mode = 4, TotalScore = 77 };
        var merged = ScoreMigration.MergeStatistics([normal, relax, autopilot, taiko, otherPlayer]);
        var repeated = ScoreMigration.MergeStatistics(merged);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(merged, Has.Length.EqualTo(3));
            Assert.That(normal.TotalScore, Is.EqualTo(600));
            Assert.That(normal.PlayCount, Is.EqualTo(9));
            Assert.That(normal.PlayTime, Is.EqualTo(70));
            Assert.That(normal.ReplayViews, Is.EqualTo(3));
            Assert.That(normal.MaxCombo, Is.EqualTo(50));
            Assert.That(normal.Total300s, Is.EqualTo(30));
            Assert.That(normal.Total100s, Is.EqualTo(4));
            Assert.That(normal.Total50s, Is.EqualTo(2));
            Assert.That(normal.TotalGekis, Is.EqualTo(7));
            Assert.That(normal.TotalKatus, Is.EqualTo(8));
            Assert.That(normal.ProfileDefaultMods, Is.EqualTo("HD;"));
            Assert.That(taiko.TotalScore, Is.EqualTo(99));
            Assert.That(repeated.Single(s => s.PlayerId == 2).TotalScore, Is.EqualTo(77));
            Assert.That(repeated.All(s => s.Mode < 4), Is.True);
        }
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
