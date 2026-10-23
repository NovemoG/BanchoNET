using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Utils.Scores;

public static class ScoreBestSelection
{
    public static void Assign(
        IReadOnlyCollection<ScoreDto> scores
    ) {
        var passed = scores.Where(s => s.Status != SubmissionStatus.Failed).ToArray();
        foreach (var score in scores)
        {
            score.Status = score.TotalScoreStatus = score.Status == SubmissionStatus.Failed
                ? SubmissionStatus.Failed
                : SubmissionStatus.Submitted;
            score.Preserve = score.IsPinned;
        }

        foreach (var group in passed.GroupBy(s => s.ModKeys))
        {
            group.OrderByDescending(s => s.PP).ThenBy(s => s.PlayTime).ThenBy(s => s.Id).First().Status = SubmissionStatus.BestWithMods;
            group.OrderByDescending(s => s.TotalScore).ThenBy(s => s.PlayTime).ThenBy(s => s.Id).First().TotalScoreStatus = SubmissionStatus.BestWithMods;
        }

        if (passed.Length > 0)
        {
            passed.OrderByDescending(s => s.PP).ThenBy(s => s.PlayTime).ThenBy(s => s.Id).First().Status = SubmissionStatus.Best;
            passed.OrderByDescending(s => s.TotalScore).ThenBy(s => s.PlayTime).ThenBy(s => s.Id).First().TotalScoreStatus = SubmissionStatus.Best;
        }

        foreach (var score in scores)
            score.Preserve |= score.Status >= SubmissionStatus.BestWithMods || score.TotalScoreStatus >= SubmissionStatus.BestWithMods;
    }
}