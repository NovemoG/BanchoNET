namespace BanchoNET.Core.Models.History;

/// <summary>
/// A single point to append to a player's history series.
/// <paramref name="Date"/> must already be normalised to the start of its bucket.
/// </summary>
public readonly record struct PlayerHistorySample(
    int PlayerId,
    byte Mode,
    HistoryMetric Metric,
    HistoryGranularity Granularity,
    DateOnly Date,
    double Value);
