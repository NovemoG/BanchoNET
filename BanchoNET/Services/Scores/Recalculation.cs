using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Scores;

public sealed partial class ScoreService
{
    private async Task UpdateScoreGroup(
        int playerId,
        int mapId,
        GameMode mode,
        long? removeId = null,
        long? recalculateId = null,
        bool recalculateAll = false
    ) {
        var beatmap = await beatmaps.GetBeatmap(mapId)
                  ?? throw new InvalidOperationException($"Beatmap {mapId} was not found.");
        
        if ((recalculateAll || recalculateId != null) && !await beatmaps.EnsureLocalBeatmapFile(beatmap))
            throw new InvalidOperationException($"Cannot recalculate scores: beatmap file {mapId} is unavailable.");

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await ScoreProcessing.LockPlayer(db, playerId, mode);
            
            var key = (long)playerId << 32 | (uint)mapId;
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})");

            var validScores = await db.Scores
                .Include(s => s.Beatmap)
                .Where(s => s.PlayerId == playerId
                            && s.MapId == mapId && s.Mode == mode
                            && (s.BeatmapMD5 == beatmap.Checksum && s.Status != SubmissionStatus.Failed
                                || s.Id == removeId
                                || s.Id == recalculateId))
                .ToListAsync();

            var candidates = validScores.Where(s => s.BeatmapMD5 == beatmap.Checksum).ToList();
            var visible = candidates.Any(s => s.Status >= SubmissionStatus.BestWithMods || s.TotalScoreStatus >= SubmissionStatus.BestWithMods);

            foreach (var score in validScores)
            {
                if (score.Id == removeId)
                {
                    db.Scores.Remove(score);
                    candidates.Remove(score);
                }
                else if (recalculateAll && score.Status != SubmissionStatus.Failed || score.Id == recalculateId)
                {
                    score.CalculatePerformance(score.Beatmap);
                    score.PP = MathF.Round(score.PP, 3, MidpointRounding.AwayFromZero);
                }
            }
            if (visible) ScoreBestSelection.Assign(candidates);
            
            await db.SaveChangesAsync();
            
            if (await db.Stats.SingleOrDefaultAsync(s => s.PlayerId == playerId && s.Mode == (byte)mode) is { } stats)
            {
                await db.Entry(stats).ReloadAsync();
                await ScoreStatistics.Recalculate(db, stats);
            }
            
            await transaction.CommitAsync();
        }
        await RefreshRank(playerId, mode);
    }

    public async Task RemoveScore(
        long id
    ) {
        if (await scores.GetScore(id) is { } score)
            await UpdateScoreGroup(score.PlayerId, score.MapId, score.Mode, removeId: id);
    }

    public async Task<bool> RecalculateScore(
        long id
    ) {
        var score = await scores.GetScore(id);
        if (score == null) return false;
        
        await UpdateScoreGroup(score.PlayerId, score.MapId, score.Mode, recalculateId: id);
        return true;
    }

    public async Task RecalculateAllScores()
    {
        var groups = await db.Scores
            .Where(s => s.Status >= SubmissionStatus.BestWithMods || s.TotalScoreStatus >= SubmissionStatus.BestWithMods)
            .Select(s => new { s.PlayerId, s.MapId, s.Mode })
            .Distinct()
            .ToListAsync();
        
        foreach (var group in groups)
            await UpdateScoreGroup(group.PlayerId, group.MapId, group.Mode, recalculateAll: true);
    }
}