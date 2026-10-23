using System.Text.Json;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
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
