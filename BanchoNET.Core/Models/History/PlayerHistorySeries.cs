using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.History;

/// <summary>
/// Reshapes stored samples into what the API serves.
/// </summary>
public static class PlayerHistorySeries
{
    /// <summary>
    /// Turns a cumulative series into per-period counts. Deleting scores can lower a lifetime
    /// total, so a negative step is clamped to zero rather than reported as negative activity.
    /// </summary>
    /// <param name="ascendingSamples">Samples for a single metric, ordered by date.</param>
    public static List<(DateOnly Date, double Count)> ToPeriodCounts(
        IReadOnlyList<PlayerHistoryDto> ascendingSamples
    ) {
        var counts = new List<(DateOnly, double)>(ascendingSamples.Count);
        double previous = 0;

        foreach (var sample in ascendingSamples)
        {
            counts.Add((sample.Date, Math.Max(0, sample.Value - previous)));
            previous = sample.Value;
        }

        return counts;
    }

    /// <summary>
    /// Projects a daily series onto a fixed window ending at <paramref name="endDate"/>, so index
    /// <c>i</c> means the same date regardless of how many samples exist. Days with no sample
    /// carry the last known value forward; days before the first sample stay 0.
    /// </summary>
    /// <param name="ascendingSamples">Samples for a single metric, ordered by date.</param>
    public static int[] ToDailyWindow(
        IReadOnlyList<PlayerHistoryDto> ascendingSamples,
        DateOnly endDate,
        int days
    ) {
        if (days <= 0) return [];

        var start = endDate.AddDays(-(days - 1));
        var byDate = new Dictionary<DateOnly, int>(days);
        var lastBeforeWindow = 0;

        foreach (var sample in ascendingSamples)
        {
            if (sample.Date < start)
            {
                lastBeforeWindow = (int)sample.Value;
                continue;
            }

            if (sample.Date > endDate) break;

            byDate[sample.Date] = (int)sample.Value;
        }

        var window = new int[days];
        var current = lastBeforeWindow;

        for (var i = 0; i < days; i++)
        {
            if (byDate.TryGetValue(start.AddDays(i), out var value))
                current = value;

            window[i] = current;
        }

        return window;
    }

    /// <summary>
    /// Builds the per-month counts a profile displays, one entry for every month from
    /// <paramref name="firstMonth"/> through <paramref name="currentMonth"/>, with months the
    /// player did not play reported as 0. A player with no recorded activity at all gets
    /// an empty series.
    /// <para>
    /// The month in progress has not been sampled yet, so it is derived from the live counter.
    /// When a monthly sample is missing entirely its activity falls into the next month's one.
    /// </para>
    /// </summary>
    /// <param name="ascendingSamples">Cumulative monthly samples, ordered by date.</param>
    /// <param name="firstMonth">Account creation month, so the graph starts at signup.</param>
    /// <param name="currentMonth">The month in progress.</param>
    /// <param name="currentCumulative">Live lifetime counter, covering the month in progress.</param>
    /// <param name="trimLeadingEmptyMonths">Starts the series at the first month with activity.</param>
    public static List<MonthlyCount> ToMonthlySeries(
        IReadOnlyList<PlayerHistoryDto> ascendingSamples,
        DateOnly firstMonth,
        DateOnly currentMonth,
        double currentCumulative,
        bool trimLeadingEmptyMonths = false
    ) {
        firstMonth = StartOfMonth(firstMonth);
        currentMonth = StartOfMonth(currentMonth);

        var byMonth = new Dictionary<DateOnly, double>(ascendingSamples.Count + 1);
        foreach (var (date, count) in ToPeriodCounts(WithLiveCounter(ascendingSamples, currentMonth, currentCumulative)))
            byMonth[StartOfMonth(date)] = count;

        // History predating the recorded join date still belongs on the graph.
        var start = firstMonth;
        if (ascendingSamples.Count > 0)
        {
            var earliest = StartOfMonth(ascendingSamples[0].Date);
            if (earliest < start) start = earliest;
        }

        var series = new List<MonthlyCount>();

        for (var month = start; month <= currentMonth; month = month.AddMonths(1))
            series.Add(new MonthlyCount(month, (int)byMonth.GetValueOrDefault(month)));

        var firstActivity = series.FindIndex(m => m.Count > 0);
        if (firstActivity < 0) return [];

        return trimLeadingEmptyMonths
            ? series.GetRange(firstActivity, series.Count - firstActivity)
            : series;
    }

    /// <summary>
    /// Appends the live counter as a sample for the month in progress, so it inverts to a period
    /// count through the same path as every stored sample. Skipped when a sample already covers
    /// that month, which would otherwise double-count it.
    /// </summary>
    private static List<PlayerHistoryDto> WithLiveCounter(
        IReadOnlyList<PlayerHistoryDto> ascendingSamples,
        DateOnly currentMonth,
        double currentCumulative
    ) {
        var samples = new List<PlayerHistoryDto>(ascendingSamples);

        if (samples.Count > 0 && StartOfMonth(samples[^1].Date) >= currentMonth)
            return samples;

        samples.Add(new PlayerHistoryDto
        {
            Date = currentMonth,
            Value = currentCumulative
        });

        return samples;
    }

    private static DateOnly StartOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
}