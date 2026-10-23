using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Scores;

namespace BanchoNET.Tests;

public class ScoreBestSelectionTests
{
    [Test]
    public void IndependentWinnersAndModWinnersSurviveDemotion()
    {
        var scoreBest = Score(1, 900000, 200);
        var ppBest = Score(2, 800000, 300);
        var modBest = Score(3, 700000, 100, "HD;");
        var nextPpBest = Score(4, 750000, 400);
        ScoreBestSelection.Assign([scoreBest, ppBest, modBest]);
        Assert.That(scoreBest.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Best));
        Assert.That(ppBest.Status, Is.EqualTo(SubmissionStatus.Best));
        Assert.That(modBest.Status, Is.EqualTo(SubmissionStatus.BestWithMods));
        ScoreBestSelection.Assign([scoreBest, ppBest, modBest, nextPpBest]);
        Assert.That(scoreBest.Preserve, Is.True);
        Assert.That(scoreBest.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Best));
        Assert.That(nextPpBest.Status, Is.EqualTo(SubmissionStatus.Best));
        Assert.That(ppBest.Preserve, Is.False);
        Assert.That(ppBest.Status, Is.EqualTo(SubmissionStatus.Submitted));
        Assert.That(modBest.Preserve, Is.True);
    }

    [Test]
    public void OnePlayCanWinBothMetricsAndTiesKeepEarlierPlay()
    {
        var winner = Score(1, 900000, 300);
        var tie = Score(2, 900000, 300);
        var fail = Score(3, 1000000, 400);
        fail.Status = SubmissionStatus.Failed;
        ScoreBestSelection.Assign([tie, fail, winner]);
        Assert.That(winner.Status, Is.EqualTo(SubmissionStatus.Best));
        Assert.That(winner.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Best));
        Assert.That(tie.Preserve, Is.False);
        Assert.That(fail.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Failed));
    }

    [Test]
    public void TiesKeepEarlierDateOverLowerId()
    {
        var later = Score(1, 900000, 300);
        later.PlayTime = DateTimeOffset.UnixEpoch.AddDays(1);
        var earlier = Score(2, 900000, 300);
        earlier.PlayTime = DateTimeOffset.UnixEpoch;
        ScoreBestSelection.Assign([later, earlier]);
        Assert.That(earlier.Status, Is.EqualTo(SubmissionStatus.Best));
        Assert.That(earlier.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Best));
        Assert.That(later.Status, Is.EqualTo(SubmissionStatus.Submitted));
    }

    [Test]
    public void PinnedPlaySurvivesLosingBothMetrics()
    {
        var pinned = Score(1, 600000, 100);
        pinned.IsPinned = true;
        ScoreBestSelection.Assign([pinned, Score(2, 900000, 300)]);
        Assert.That(pinned.Status, Is.EqualTo(SubmissionStatus.Submitted));
        Assert.That(pinned.TotalScoreStatus, Is.EqualTo(SubmissionStatus.Submitted));
        Assert.That(pinned.Preserve, Is.True);
    }

    private static ScoreDto Score(long id, long total, float pp, string mods = "") => new()
    {
        Id = id, BeatmapMD5 = "", MapId = 1, PlayerId = 1, ModKeys = mods,
        TotalScore = total, PP = pp, Status = SubmissionStatus.Submitted
    };
}
