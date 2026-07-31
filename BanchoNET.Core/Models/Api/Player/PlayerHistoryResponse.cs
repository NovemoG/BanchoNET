using BanchoNET.Core.Models.History;

namespace BanchoNET.Core.Models.Api.Player;

/// <summary>
/// Full stored history for a player and mode.
/// </summary>
public sealed class PlayerHistoryResponse
{
    public int UserId { get; set; }
    public string Mode { get; set; } = "osu";
    public List<PlayerHistorySeriesResponse> Series { get; set; } = [];
}

public sealed class PlayerHistorySeriesResponse
{
    public HistoryMetric Metric { get; set; }
    public HistoryGranularity Granularity { get; set; }

    /// <summary>
    /// True when values are running totals, in which case consecutive differences give the
    /// per-period activity.
    /// </summary>
    public bool Cumulative { get; set; }

    public List<PlayerHistoryPoint> Points { get; set; } = [];
}

public sealed class PlayerHistoryPoint
{
    public DateOnly Date { get; set; }
    public double Value { get; set; }
}