namespace BanchoNET.Core.Models.History;

/// <summary>
/// What a history sample measures. Stored as a smallint, so values must never be reused
/// once they have been written — append new members, don't renumber existing ones.
/// </summary>
public enum HistoryMetric : short
{
    /// <summary>Global leaderboard position. Absolute value; lower is better.</summary>
    GlobalRank = 0,

    /// <summary>Country leaderboard position. Absolute value; lower is better.</summary>
    CountryRank = 1,

    /// <summary>Lifetime play count. Cumulative — per-period counts are read as deltas.</summary>
    PlayCount = 2,

    /// <summary>Lifetime replays watched by others. Cumulative.</summary>
    ReplayViews = 3,

    /// <summary>Performance points. Absolute value.</summary>
    Pp = 4,

    /// <summary>Lifetime ranked score. Cumulative.</summary>
    RankedScore = 5,

    /// <summary>Lifetime total score. Cumulative.</summary>
    TotalScore = 6,

    /// <summary>Hit accuracy as a percentage. Absolute value.</summary>
    Accuracy = 7,
}
