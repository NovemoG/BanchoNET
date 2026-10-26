using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Services;

public interface IScoreService
{
    Task<long> ReserveScoreId();
    Task RemoveScore(long id);
    Task<bool> RecalculateScore(long id);
    Task RecalculateAllScores();
    
    Task<ScoreSubmissionResult?> Submit(ScoreSubmission submission);

    Task<ScoreCompletionResult> Complete(
        long scoreId,
        int? playTimeSeconds = null,
        Func<string, Task>? writeReplay = null
    );
}