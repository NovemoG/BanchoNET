using BanchoNET.Core.Utils;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Tests;

public class LeaderboardOrderingTests
{
    [TestCase(LeaderboardSort.Score, 1)]
    [TestCase(LeaderboardSort.PP, 2)]
    [TestCase(LeaderboardSort.Accuracy, 2)]
    [TestCase(LeaderboardSort.MaxCombo, 2)]
    [TestCase(LeaderboardSort.Misses, 2)]
    [TestCase(LeaderboardSort.Date, 2)]
    public void SortingDoesNotReplaceScoreValues(LeaderboardSort sort, long firstId)
    {
        var first = Score(1, 1, 900000, 100);
        first.Acc = 95;
        first.MaxCombo = 100;
        first.Statistics[HitResult.Miss] = 3;
        first.MissCount = 3;
        first.PlayTime = DateTimeOffset.UnixEpoch;

        var second = Score(2, 2, 800000, 200);
        second.Acc = 99;
        second.MaxCombo = 200;
        second.Statistics[HitResult.Miss] = 0;
        second.PlayTime = DateTimeOffset.UnixEpoch.AddDays(1);

        Assert.That(LeaderboardOrdering.Order([first, second], sort).First().Id, Is.EqualTo(firstId));
        Assert.That(first.LegacyTotalScore, Is.EqualTo(900000));
        Assert.That(second.PP, Is.EqualTo(200));
    }

    [Test]
    public void TiesAreResolvedByEarlierDateOnly()
    {
        var later = Score(1, 1, 900000, 100);
        later.PlayTime = DateTimeOffset.UnixEpoch.AddDays(1);
        var earlier = Score(2, 2, 800000, 100);
        earlier.PlayTime = DateTimeOffset.UnixEpoch;
        Assert.That(LeaderboardOrdering.Order([later, earlier], LeaderboardSort.PP).First().Id, Is.EqualTo(2));
    }

    [TestCase(LeaderboardSort.Score)]
    [TestCase(LeaderboardSort.PP)]
    [TestCase(LeaderboardSort.Accuracy)]
    [TestCase(LeaderboardSort.MaxCombo)]
    [TestCase(LeaderboardSort.Misses)]
    [TestCase(LeaderboardSort.Date)]
    public void PositionsMatchOrdering(LeaderboardSort sort)
    {
        ScoreDto[] scores =
        [
            Score(1, 1, 900000, 100), Score(2, 2, 900000, 100), Score(3, 3, 800000, 200),
            Score(4, 4, 800000, 200), Score(5, 5, 700000, 50)
        ];
        for (var i = 0; i < scores.Length; i++)
        {
            scores[i].Acc = i < 2 ? 99 : 95;
            scores[i].MaxCombo = i % 2 == 0 ? 100 : 200;
            scores[i].MissCount = i < 3 ? 0 : 2;
            scores[i].PlayTime = DateTimeOffset.UnixEpoch.AddDays(i % 3);
        }
        var ordered = LeaderboardOrdering.Order(scores, sort).ToList();
        foreach (var score in scores)
            Assert.That(LeaderboardOrdering.Ahead(scores.AsQueryable(), score, sort).Count() + 1, Is.EqualTo(ordered.IndexOf(score) + 1));
    }

    private static ScoreDto Score(long id, int playerId, long totalScore, float pp) => new()
    {
        Id = id,
        PlayerId = playerId,
        BeatmapMD5 = "",
        ModKeys = "",
        TotalScore = totalScore,
        LegacyTotalScore = totalScore,
        PP = pp,
        Status = SubmissionStatus.Best
    };
}
