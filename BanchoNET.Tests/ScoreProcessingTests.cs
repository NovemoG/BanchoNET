using System.Text.Json;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;

namespace BanchoNET.Tests;

public class ScoreProcessingTests
{
    [TestCase(GameMode.Standard)]
    [TestCase(GameMode.Taiko)]
    [TestCase(GameMode.Catch)]
    [TestCase(GameMode.Mania)]
    public void EquivalentStableAndLazerScoresContributeTheSameLifetimeStatistics(GameMode mode)
    {
        var legacy = Row(mode);
        legacy.Statistics = new Score
        {
            Mode = mode, Count300 = 100, Count100 = 20, Count50 = 3,
            Gekis = 50, Katus = 4, Misses = 2
        }.GetStatistics();
        legacy.LegacyTotalScore = 9000000;
        var lazer = Row(mode);
        lazer.Statistics = legacy.Statistics.ToDictionary();
        lazer.LazerMods = "";
        var first = new StatsDto();
        var second = new StatsDto();
        ScoreProcessing.AddStatistics(first, legacy, 90);
        ScoreProcessing.AddStatistics(second, lazer, 90);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.TotalScore, Is.EqualTo(777000), "Cumulative score uses standardised totals.");
            Assert.That(first.PlayCount, Is.EqualTo(1));
            Assert.That(first.PlayTime, Is.EqualTo(90));
            Assert.That(Counters(first), Is.EqualTo(Counters(second)));
            if (mode == GameMode.Mania)
            {
                Assert.That(second.TotalGekis, Is.EqualTo(50));
                Assert.That(second.TotalKatus, Is.EqualTo(4));
            }
            if (mode == GameMode.Catch)
            {
                Assert.That(second.Total300s, Is.EqualTo(100));
                Assert.That(second.Total100s, Is.EqualTo(20));
                Assert.That(second.Total50s, Is.EqualTo(3));
            }
        }
    }

    [Test]
    public void DeferredDurationIsAppliedOnceAndDoesNotCountThePlayAgain()
    {
        var row = Row(GameMode.Standard);
        row.LazerMods = "";
        var stats = new StatsDto();
        ScoreProcessing.AddStatistics(stats, row, null);
        Assert.That(row.PlayTimeRecorded, Is.False);
        Assert.That(ScoreProcessing.RecordDuration(stats, row, null), Is.False);
        Assert.That(ScoreProcessing.RecordDuration(stats, row, 90), Is.True);
        Assert.That(ScoreProcessing.RecordDuration(stats, row, 120), Is.False);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stats.PlayCount, Is.EqualTo(1));
            Assert.That(stats.TotalScore, Is.EqualTo(777000));
            Assert.That(stats.PlayTime, Is.EqualTo(90));
            Assert.That(row.TimeElapsed, Is.EqualTo(90));
        }
    }

    [Test]
    public void HistoricalRowsDoNotReceivePlaytimeAgain()
    {
        var row = new ScoreDto { BeatmapMD5 = "", ModKeys = "" };
        var stats = new StatsDto { PlayTime = 100 };
        Assert.That(ScoreProcessing.RecordDuration(stats, row, 90), Is.False);
        Assert.That(stats.PlayTime, Is.EqualTo(100));
    }

    [TestCase(SubmissionStatus.Failed, true)]
    [TestCase(SubmissionStatus.Submitted, false)]
    public void IneligiblePlaysStillCountWithoutIncreasingRankedMaxCombo(SubmissionStatus status, bool ranked)
    {
        var row = Row(GameMode.Standard);
        row.Status = status;
        row.Ranked = ranked;
        var stats = new StatsDto { MaxCombo = 5 };
        ScoreProcessing.AddStatistics(stats, row, 10);
        Assert.That(stats.PlayCount, Is.EqualTo(1));
        Assert.That(stats.MaxCombo, Is.EqualTo(5));
    }

    [TestCase(1d, 90000, 90000d, 90)]
    [TestCase(1.5d, 135000, 135000d, 90)]
    [TestCase(0.75d, 67500, 67500d, 90)]
    public void BothClientDurationsUseElapsedSeconds(double rate, int legacyTime, double frameTime, int expected)
    {
        Assert.That(ScoreProcessing.LegacyPlayTimeSeconds(legacyTime, rate), Is.EqualTo(expected));
        Assert.That(ScoreProcessing.LazerPlayTimeSeconds(frameTime + 5000, 5, rate), Is.EqualTo(expected));
        Assert.That(ScoreProcessing.LegacyPlayTimeSeconds(-1, rate), Is.Zero);
        Assert.That(ScoreProcessing.LazerPlayTimeSeconds(1000, 5, rate), Is.Zero);
    }

    [TestCase("DT", 1.5d)]
    [TestCase("NC", 1.5d)]
    [TestCase("HT", 0.75d)]
    [TestCase("DC", 0.75d)]
    public void SubmissionAndReplayUseTheSameClockRate(string acronym, double expected)
    {
        Mod[] mods = [new(acronym, true)];
        Assert.That(mods.GetClockRate(), Is.EqualTo(expected));
        mods[0].Settings["speed_change"] = JsonSerializer.Deserialize<JsonElement>("1.2");
        Assert.That(mods.GetClockRate(), Is.EqualTo(1.2));
        mods[0].Settings["speed_change"] = "1.2";
        Assert.That(mods.GetClockRate(), Is.EqualTo(1.2));
    }

    [Test]
    public void ApiResultUsesStoredStatisticsAndFractionalAccuracy()
    {
        var row = Row(GameMode.Mania);
        row.Id = 42;
        row.Acc = 98.5f;
        row.Statistics[HitResult.Perfect] = 50;
        var api = new ApiScore { Accuracy = 100, Statistics = new() { [HitResult.Perfect] = 999 } };
        ScoreSubmissionMapping.Apply(row, api);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(api.Id, Is.EqualTo(42));
            Assert.That(api.Accuracy, Is.EqualTo(0.985).Within(0.000001));
            Assert.That(api.Statistics[HitResult.Perfect], Is.EqualTo(50));
            Assert.That(api.TotalScore, Is.EqualTo(777000));
        }
    }

    [Test]
    public void LazerMappingKeepsTheReservedScoreIdForInitialSubmissionAndRetries()
    {
        var map = new Beatmap(new BeatmapDto { Id = 1, MD5 = "map" },
            new Beatmapset(new BeatmapsetDto { Beatmaps = [] }));
        var score = new ApiScore { Id = 42, UserId = 2, StartedAt = DateTimeOffset.UnixEpoch, Accuracy = 99 };
        var first = ScoreSubmissionMapping.FromLazer(score, map);
        score.EndedAt = DateTimeOffset.UtcNow;
        score.TotalScore = 999999;
        var retry = ScoreSubmissionMapping.FromLazer(score, map);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Score.Id, Is.EqualTo(42));
            Assert.That(retry.Score.Id, Is.EqualTo(42));
            Assert.That(ScoreProcessing.MatchSubmission(retry.Score).Compile()(first.Score), Is.True);
        }
    }

    [Test]
    public void StableRetriesUseThePlayerChecksumInsteadOfDatabaseIdOrScoreValues()
    {
        var incoming = Row(GameMode.Standard);
        incoming.PlayerId = 1;
        incoming.OnlineChecksum = new string('a', 32);
        var existing = Row(GameMode.Standard);
        existing.Id = 42;
        existing.PlayerId = 1;
        existing.OnlineChecksum = incoming.OnlineChecksum;
        existing.TotalScore = 12345;
        var matches = ScoreProcessing.MatchSubmission(incoming).Compile();
        Assert.That(matches(existing), Is.True);
        existing.PlayerId = 2;
        Assert.That(matches(existing), Is.False);
        existing.PlayerId = 1;
        existing.LazerMods = "";
        Assert.That(matches(existing), Is.False);
        existing.LazerMods = null;
        existing.OnlineChecksum = new string('b', 32);
        Assert.That(matches(existing), Is.False);
    }

    [Test]
    public void LazerRetriesMatchOnlyTheReservedId()
    {
        var incoming = Row(GameMode.Standard);
        incoming.Id = 42;
        incoming.LazerMods = "";
        var existing = Row(GameMode.Standard);
        existing.Id = 42;
        existing.LazerMods = "";
        existing.TotalScore = 12345;
        var matches = ScoreProcessing.MatchSubmission(incoming).Compile();
        Assert.That(matches(existing), Is.True);
        existing.Id = 43;
        Assert.That(matches(existing), Is.False);
    }

    private static long[] Counters(StatsDto stats) => [
        stats.TotalScore, stats.PlayCount, stats.PlayTime, stats.MaxCombo,
        stats.Total300s, stats.Total100s, stats.Total50s, stats.TotalGekis, stats.TotalKatus];

    private static ScoreDto Row(GameMode mode) => new()
    {
        BeatmapMD5 = "", ModKeys = "", Mode = mode, TotalScore = 777000,
        Status = SubmissionStatus.Submitted, Ranked = true, MaxCombo = 100,
        PlayTimeRecorded = false
    };
}
