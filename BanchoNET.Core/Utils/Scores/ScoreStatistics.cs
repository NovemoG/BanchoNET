using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Utils.Scores;

public static class ScoreStatistics
{
    public static async Task Recalculate(
        BanchoDbContext db,
        StatsDto stats
    ) {
        var mode = (GameMode)stats.Mode;
        float weightedPp = 0, weightedAcc = 0;
        
        var eligible = db.Scores.Where(s => s.PlayerId == stats.PlayerId && s.Mode == mode && s.Ranked);
        var best = await eligible
            .Where(s => s.Status == SubmissionStatus.Best)
            .OrderByDescending(s => s.PP)
            .Take(AppSettings.TopPlaysCount)
            .ToListAsync();

        for (var i = 0; i < best.Count; i++)
        {
            var weight = MathF.Pow(0.95f, i);
            weightedPp += best[i].PP * weight;
            weightedAcc += best[i].Acc * weight;
        }
        
        var accWeight = best.Count == 0 ? 0 : 1f / (20 * (1 - MathF.Pow(0.95f, best.Count)));
        var bonus = (417 - 1f / 3) * (1 - MathF.Pow(0.995f, Math.Min(1000, best.Count)));
        
        stats.Accuracy = weightedAcc * accWeight;
        stats.PP = (int)MathF.Round(weightedPp + bonus);
        stats.RankedScore = await eligible.Where(s => s.TotalScoreStatus == SubmissionStatus.Best).SumAsync(s => s.TotalScore);
        
        var grades = await eligible
            .Where(s => s.Status >= SubmissionStatus.BestWithMods)
            .GroupBy(s => s.Grade)
            .Select(g => new { Grade = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Grade, g => g.Count);
        
        stats.XHCount = grades.GetValueOrDefault(Grade.XH);
        stats.XCount = grades.GetValueOrDefault(Grade.X);
        stats.SHCount = grades.GetValueOrDefault(Grade.SH);
        stats.SCount = grades.GetValueOrDefault(Grade.S);
        stats.ACount = grades.GetValueOrDefault(Grade.A);
        
        await db.SaveChangesAsync();
        await db.Players.Where(p => p.Id == stats.PlayerId)
            .ExecuteUpdateAsync(p => p.SetProperty(v => v.TopPlaysCount, best.Count));
    }
}