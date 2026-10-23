using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Scores;

public sealed record ScoreSubmission(
    ScoreDto Score,
    int? PlayTimeSeconds,
    Dictionary<HitResult, int>? MaximumStatistics = null
);

public sealed record ScoreSubmissionResult(
    ScoreDto Score,
    StatsDto Statistics,
    StatsDto PreviousStatistics,
    int PreviousRank,
    ScoreDto? PreviousBest,
    int Position,
    int PreviousPosition,
    bool IsNew,
    bool IsFirstPlace,
    ScoreDto? PreviousFirstPlace
);

public sealed record ScoreLeaderboard(
    List<ScoreDto> Scores,
    int Count,
    ScoreDto? UserScore,
    int Position
);

public sealed record ScoreLeaderboardPreferences(bool PerformancePoints, LeaderboardSort Sorting);

public sealed record ScoreCompletionResult(bool FirstCompletion, bool HasReplay);