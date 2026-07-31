using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.History;

/// <summary>
/// Conversions between stored samples and the shapes the API and the importer need.
/// Kept free of any data access so the date arithmetic can be tested directly — it is the
/// part of the import that cannot be checked by looking at the result.
/// </summary>
public static class PlayerHistorySeries
{
    /// <summary>
    /// Start of the bucket <paramref name="stepsBack"/> periods before <paramref name="anchor"/>.
    /// </summary>
    public static DateOnly BucketBefore(
        DateOnly anchor,
        HistoryGranularity granularity,
        int stepsBack
    ) {
        return granularity switch
        {
            HistoryGranularity.Monthly => anchor.AddMonths(-stepsBack),
            _ => anchor.AddDays(-stepsBack)
        };
    }

    /// <summary>
    /// Dates a positional array whose values are already absolute measurements (rank, pp),
    /// placing the last entry on <paramref name="anchor"/> and each earlier one a bucket back.
    /// </summary>
    public static IEnumerable<PlayerHistorySample> FromAbsoluteEntries(
        int playerId,
        byte mode,
        HistoryMetric metric,
        HistoryGranularity granularity,
        IReadOnlyList<int> entries,
        DateOnly anchor
    ) {
        for (var i = 0; i < entries.Count; i++)
        {
            yield return new PlayerHistorySample(
                playerId,
                mode,
                metric,
                granularity,
                BucketBefore(anchor, granularity, entries.Count - 1 - i),
                entries[i]);
        }
    }

    /// <summary>
    /// Dates a positional array whose values are per-period counts, summing them forward into
    /// the cumulative form the series stores.
    /// </summary>
    public static IEnumerable<PlayerHistorySample> FromPeriodCounts(
        int playerId,
        byte mode,
        HistoryMetric metric,
        HistoryGranularity granularity,
        IReadOnlyList<int> entries,
        DateOnly anchor
    ) {
        double running = 0;

        for (var i = 0; i < entries.Count; i++)
        {
            running += entries[i];

            yield return new PlayerHistorySample(
                playerId,
                mode,
                metric,
                granularity,
                BucketBefore(anchor, granularity, entries.Count - 1 - i),
                running);
        }
    }

    /// <summary>
    /// Turns a cumulative series back into per-period counts — the inverse of
    /// <see cref="FromPeriodCounts"/>, and what the profile actually displays.
    /// <para>
    /// Deleting scores can lower a cumulative total, so a negative step is clamped to zero
    /// rather than reported as negative activity.
    /// </para>
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
    /// Projects a daily series onto a fixed window ending at <paramref name="endDate"/>, so
    /// index <c>i</c> always means the same date regardless of how many samples exist.
    /// <para>
    /// Days with no sample carry the last known value forward rather than reading as rank 0,
    /// which would otherwise draw a cliff to the bottom of the chart for every missed run.
    /// Days before the player's first sample stay 0 — there is nothing to carry.
    /// </para>
    /// </summary>
    /// <param name="ascendingSamples">Samples for a single metric, ordered by date.</param>
    public static int[] ToDailyWindow(
        IReadOnlyList<PlayerHistoryDto> ascendingSamples,
        DateOnly endDate,
        int days
    ) {
        if (days <= 0) return [];

        var window = new int[days];
        var start = endDate.AddDays(-(days - 1));
        var byDate = new Dictionary<DateOnly, int>(days);
        var carried = 0;

        foreach (var sample in ascendingSamples)
        {
            // Ordering matters: samples before the window set the value carried into day one.
            if (sample.Date < start)
            {
                carried = (int)sample.Value;
                continue;
            }

            if (sample.Date > endDate) break;

            byDate[sample.Date] = (int)sample.Value;
        }

        for (var i = 0; i < days; i++)
        {
            if (byDate.TryGetValue(start.AddDays(i), out var value))
                carried = value;

            window[i] = carried;
        }

        return window;
    }

    /// <summary>
    /// Builds the per-month counts a profile displays: one entry for every month from
    /// <paramref name="firstMonth"/> to <paramref name="currentMonth"/> inclusive, with
    /// months the player did not play reported as 0 rather than omitted.
    /// <para>
    /// A player with no recorded activity at all gets an empty series instead, so a profile
    /// that has never been played shows nothing rather than a flat row of zeros.
    /// </para>
    /// </summary>
    /// <param name="ascendingSamples">Cumulative monthly samples, ordered by date.</param>
    /// <param name="firstMonth">Usually the account creation month, so the graph starts at signup.</param>
    /// <param name="currentMonth">The month in progress, which has not been sampled yet.</param>
    /// <param name="currentCumulative">
    /// Live lifetime counter, used to show activity in the month in progress.
    /// </param>
    /// <param name="trimLeadingEmptyMonths">
    /// Starts the series at the first month with activity instead of at
    /// <paramref name="firstMonth"/>. Suits metrics where the months before anything happened
    /// carry no meaning — replays watched by others, as opposed to play count, where the run
    /// of empty months since signup is itself information.
    /// </param>
    public static List<(DateOnly Month, double Count)> ToMonthlySeries(
        IReadOnlyList<PlayerHistoryDto> ascendingSamples,
        DateOnly firstMonth,
        DateOnly currentMonth,
        double currentCumulative,
        bool trimLeadingEmptyMonths = false
    ) {
        firstMonth = StartOfMonth(firstMonth);
        currentMonth = StartOfMonth(currentMonth);

        var byMonth = new Dictionary<DateOnly, double>(ascendingSamples.Count);
        foreach (var (date, count) in ToPeriodCounts(ascendingSamples))
            byMonth[StartOfMonth(date)] = count;

        // History predating the recorded join date still belongs on the graph.
        var start = firstMonth;
        if (ascendingSamples.Count > 0)
        {
            var earliest = StartOfMonth(ascendingSamples[0].Date);
            if (earliest < start) start = earliest;
        }

        var lastCumulative = ascendingSamples.Count > 0 ? ascendingSamples[^1].Value : 0;

        var series = new List<(DateOnly Month, double Count)>();

        for (var month = start; month <= currentMonth; month = month.AddMonths(1))
        {
            if (byMonth.TryGetValue(month, out var count))
                series.Add((month, count));
            else if (month == currentMonth)
                // Not sampled until the month ends, so derive it from the live counter.
                series.Add((month, Math.Max(0, currentCumulative - lastCumulative)));
            else
                series.Add((month, 0));
        }

        var firstActivity = series.FindIndex(m => m.Count > 0);

        // Never recorded anything: an empty series rather than a column of zeros stretching
        // back to signup. Applies regardless of trimming — there is nothing to plot either way.
        if (firstActivity < 0) return [];

        // Months between the first activity and now are kept even when empty; only the run
        // before anything ever happened is dropped.
        return trimLeadingEmptyMonths
            ? series.GetRange(firstActivity, series.Count - firstActivity)
            : series;
    }

    private static DateOnly StartOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
}
