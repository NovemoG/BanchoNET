using System.Text.Json;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Tools;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;

namespace BanchoNET.Tests;

public class ScoreStatisticsMappingTests
{
    [TestCase(GameMode.Standard)]
    [TestCase(GameMode.Taiko)]
    [TestCase(GameMode.Catch)]
    [TestCase(GameMode.Mania)]
    public void StableSubmissionStoresCanonicalStatistics(GameMode mode)
    {
        var score = new Score
        {
            Mode = mode, Count300 = 10, Count100 = 5, Count50 = 2,
            Gekis = 7, Katus = 3, Misses = 1, Passed = true
        };
        var map = new Beatmap(new BeatmapDto { Id = 1, MD5 = "map" }, new Beatmapset(new BeatmapsetDto { Beatmaps = [] }));
        var stored = ScoreSubmissionMapping.FromLegacy(score, map).Score;
        var expected = Expected(mode);
        Assert.That(stored.LazerMods, Is.Null, "Canonical statistics do not change the score source.");
        Assert.That(stored.Statistics, Is.EquivalentTo(expected));
        var api = new ApiScore();
        ScoreSubmissionMapping.Apply(stored, api);
        Assert.That(api.Statistics, Is.EquivalentTo(expected));
        Assert.That(stored.GetCount100(), Is.EqualTo(5));
        if (mode == GameMode.Mania)
        {
            Assert.That(stored.GetCountGeki(), Is.EqualTo(7));
            Assert.That(stored.GetCountKatu(), Is.EqualTo(3));
        }
        if (mode == GameMode.Taiko)
            Assert.That(stored.GetCountGeki(), Is.EqualTo(10), "Both legacy strong-hit counters contribute.");
        if (mode == GameMode.Catch)
        {
            Assert.That(stored.GetCount50(), Is.EqualTo(2));
            Assert.That(stored.GetCountKatu(), Is.EqualTo(3));
        }
    }

    [TestCase(GameMode.Standard, false)]
    [TestCase(GameMode.Taiko, false)]
    [TestCase(GameMode.Catch, false)]
    [TestCase(GameMode.Mania, false)]
    [TestCase(GameMode.Taiko, true)]
    [TestCase(GameMode.Catch, true)]
    [TestCase(GameMode.Mania, true)]
    public void HistoricalStatisticsAreConvertedOnceEvenAcrossDatabaseRoundTrips(GameMode mode, bool empty)
    {
        var row = new ScoreDto
        {
            BeatmapMD5 = "", ModKeys = "", Mode = mode,
            Statistics = empty ? new() : new()
            {
                [HitResult.Great] = 10, [HitResult.Ok] = 5, [HitResult.Meh] = 2,
                [HitResult.LargeTickHit] = 7, [HitResult.SliderTailHit] = 3, [HitResult.Miss] = 1
            }
        };
        ScoreMigration.MigrateStatistics(row);
        if (!empty)
            Assert.That(row.Statistics, Is.EquivalentTo(Expected(mode)));
        else
            Assert.That(row.Statistics.Values.All(v => v == 0), Is.True);

        var converted = row.Statistics.ToDictionary();
        row.Statistics = JsonSerializer.Deserialize<Dictionary<HitResult, int>>(JsonSerializer.Serialize(row.Statistics))!;
        ScoreMigration.MigrateStatistics(row);
        Assert.That(row.Statistics, Is.EquivalentTo(converted));
    }

    [Test]
    public void HistoricalLazerStatisticsRemainUnchanged()
    {
        var row = new ScoreDto
        {
            BeatmapMD5 = "", ModKeys = "", Mode = GameMode.Standard, LazerMods = "",
            Statistics = new() { [HitResult.Great] = 10, [HitResult.LargeTickHit] = 7, [HitResult.SliderTailHit] = 3 }
        };
        var original = row.Statistics.ToDictionary();
        ScoreMigration.MigrateStatistics(row);
        Assert.That(row.Statistics, Is.EquivalentTo(original));
    }

    private static Dictionary<HitResult, int> Expected(GameMode mode)
    {
        var result = new Dictionary<HitResult, int> { [HitResult.Great] = 10, [HitResult.Miss] = 1 };
        if (mode == GameMode.Catch)
        {
            result[HitResult.LargeTickHit] = 5;
            result[HitResult.SmallTickHit] = 2;
            result[HitResult.SmallTickMiss] = 3;
        }
        else
        {
            result[HitResult.Ok] = 5;
            if (mode == GameMode.Taiko)
                result[HitResult.LargeBonus] = 10;
            else
                result[HitResult.Meh] = 2;
            if (mode == GameMode.Mania)
            {
                result[HitResult.Perfect] = 7;
                result[HitResult.Good] = 3;
            }
        }
        return result;
    }
}
