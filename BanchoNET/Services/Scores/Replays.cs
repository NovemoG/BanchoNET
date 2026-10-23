using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Scores;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Scores;

public sealed partial class ScoreService
{
    public async Task<ScoreCompletionResult> Complete(
        long scoreId,
        int? playTimeSeconds = null,
        Func<string, Task>? writeReplay = null
    ) {
        var identity = await db.Scores
            .AsNoTracking()
            .Where(s => s.Id == scoreId)
            .Select(s => new { s.PlayerId, s.Mode })
            .SingleAsync();
        
        await using var transaction = await db.Database.BeginTransactionAsync();
        await ScoreProcessing.LockPlayer(db, identity.PlayerId, identity.Mode);
        
        var score = await db.Scores.SingleAsync(s => s.Id == scoreId);
        await db.Entry(score).ReloadAsync();
        
        var firstCompletion = false;
        
        if (!score.PlayTimeRecorded && playTimeSeconds != null)
        {
            var stats = await db.Stats.SingleAsync(s => s.PlayerId == score.PlayerId && s.Mode == (byte)score.Mode);
            await db.Entry(stats).ReloadAsync();
            
            firstCompletion = ScoreProcessing.RecordDuration(stats, score, playTimeSeconds);
        }
        if (score is { Passed: true, HasReplay: false } && writeReplay != null)
        {
            var path = Storage.GetReplayPath(score.Id);
            var temporaryPath = path + ".tmp";
            
            try
            {
                await writeReplay(temporaryPath);
                File.Move(temporaryPath, path, overwrite: true);
                score.HasReplay = true;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
        
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return new ScoreCompletionResult(firstCompletion, score.HasReplay);
    }
}