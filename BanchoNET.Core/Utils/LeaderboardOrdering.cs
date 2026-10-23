using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Utils;

public static class LeaderboardOrdering
{
    public static IOrderedQueryable<ScoreDto> Order(
        IQueryable<ScoreDto> scores,
        LeaderboardSort sort
    ) {
        return (sort switch
        {
            LeaderboardSort.PP => scores.OrderByDescending(s => s.PP),
            LeaderboardSort.Accuracy => scores.OrderByDescending(s => s.Acc),
            LeaderboardSort.MaxCombo => scores.OrderByDescending(s => s.MaxCombo),
            LeaderboardSort.Misses => scores.OrderBy(s => s.MissCount),
            LeaderboardSort.Date => scores.OrderByDescending(s => s.PlayTime),
            _ => scores.OrderByDescending(s => s.TotalScore)
        }).ThenBy(s => s.PlayTime).ThenBy(s => s.Id);
    }
    
    public static IOrderedEnumerable<ScoreDto> Order(IEnumerable<ScoreDto> scores, LeaderboardSort sort)
        => Order(scores.AsQueryable(), sort).AsEnumerable().OrderBy(_ => 0);

    public static IQueryable<ScoreDto> Ahead(
        IQueryable<ScoreDto> scores,
        ScoreDto score,
        LeaderboardSort sort
    ) {
        var (id, time, pp, acc, combo, misses, total) = (score.Id, score.PlayTime, score.PP, score.Acc, score.MaxCombo, score.MissCount, score.TotalScore);

        return sort switch
        {
            LeaderboardSort.PP => scores.Where(s => s.PP > pp || s.PP == pp && (s.PlayTime < time || s.PlayTime == time && s.Id < id)),
            LeaderboardSort.Accuracy => scores.Where(s => s.Acc > acc || s.Acc == acc && (s.PlayTime < time || s.PlayTime == time && s.Id < id)),
            LeaderboardSort.MaxCombo => scores.Where(s => s.MaxCombo > combo || s.MaxCombo == combo && (s.PlayTime < time || s.PlayTime == time && s.Id < id)),
            LeaderboardSort.Misses => scores.Where(s => s.MissCount < misses || s.MissCount == misses && (s.PlayTime < time || s.PlayTime == time && s.Id < id)),
            LeaderboardSort.Date => scores.Where(s => s.PlayTime > time || s.PlayTime == time && s.Id < id),
            _ => scores.Where(s => s.TotalScore > total || s.TotalScore == total && (s.PlayTime < time || s.PlayTime == time && s.Id < id))
        };
    }
}