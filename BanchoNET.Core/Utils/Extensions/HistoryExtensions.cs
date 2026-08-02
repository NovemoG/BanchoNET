using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;

namespace BanchoNET.Core.Utils.Extensions;

public static class HistoryExtensions
{
    /// <summary>
    /// Whether a metric's samples are running totals rather than point-in-time measurements.
    /// Decides both the flag on the API response and whether a series is read as deltas.
    /// </summary>
    public static bool IsCumulative(this HistoryMetric metric) => metric switch
    {
        HistoryMetric.PlayCount => true,
        HistoryMetric.ReplayViews => true,
        HistoryMetric.GlobalRank => false,
        HistoryMetric.Pp => false,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null)
    };

    /// <summary>
    /// Groups samples into one series per metric and granularity, preserving the date ordering
    /// they arrive in.
    /// </summary>
    public static PlayerHistoryResponse ToResponse(
        this IReadOnlyList<PlayerHistoryDto> samples,
        int userId,
        string mode
    ) {
        return new PlayerHistoryResponse
        {
            UserId = userId,
            Mode = mode,
            Series = samples
                .GroupBy(s => (s.Metric, s.Granularity))
                .Select(g => new PlayerHistorySeriesResponse
                {
                    Metric = g.Key.Metric,
                    Granularity = g.Key.Granularity,
                    Cumulative = g.Key.Metric.IsCumulative(),
                    Points = g
                        .Select(s => new PlayerHistoryPoint
                        {
                            Date = s.Date,
                            Value = s.Value
                        }).ToList()
                })
                .ToList()
        };
    }
}