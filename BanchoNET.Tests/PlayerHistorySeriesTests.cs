using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Tests;

public class PlayerHistorySeriesTests
{
    private const byte Mode = 0;
    private const int PlayerId = 7;

    [Test]
    public void ToPeriodCounts_InvertsARunningSum()
    {
        int[] activity = [5, 3, 0, 7, 12];

        var stored = new List<PlayerHistoryDto>();
        var running = 0;
        var month = new DateOnly(2026, 2, 1);

        foreach (var count in activity)
        {
            running += count;
            stored.Add(Sample(month, running));
            month = month.AddMonths(1);
        }

        var recovered = PlayerHistorySeries.ToPeriodCounts(stored);

        Assert.That(recovered.Select(c => c.Count), Is.EqualTo(activity.Select(e => (double)e)));
    }

    [Test]
    public void ToPeriodCounts_ClampsNegativeStepsCausedByDeletedScores()
    {
        var samples = new List<PlayerHistoryDto>
        {
            Sample(new DateOnly(2026, 4, 1), 100),
            Sample(new DateOnly(2026, 5, 1), 80),  // scores removed, cumulative fell
            Sample(new DateOnly(2026, 6, 1), 90)
        };

        var counts = PlayerHistorySeries.ToPeriodCounts(samples);

        Assert.That(counts.Select(c => c.Count), Is.EqualTo(new double[] { 100, 0, 10 }));
    }

    [Test]
    public void ToPeriodCounts_FirstSampleIsItsOwnCount()
    {
        var counts = PlayerHistorySeries.ToPeriodCounts([Sample(new DateOnly(2026, 6, 1), 42)]);

        Assert.That(counts, Has.Count.EqualTo(1));
        Assert.That(counts[0].Count, Is.EqualTo(42));
    }

    [Test]
    public void ToDailyWindow_IndexesByDateNotByPosition()
    {
        var today = new DateOnly(2026, 7, 31);

        // Only two samples, both recent — they must land at the end of the window, not the start.
        var window = PlayerHistorySeries.ToDailyWindow(
            [RankSample(today.AddDays(-1), 120), RankSample(today, 100)],
            today,
            5);

        Assert.That(window, Is.EqualTo(new[] { 0, 0, 0, 120, 100 }));
    }

    [Test]
    public void ToDailyWindow_CarriesLastKnownValueAcrossMissedRuns()
    {
        var today = new DateOnly(2026, 7, 31);

        var window = PlayerHistorySeries.ToDailyWindow(
            [RankSample(today.AddDays(-4), 500), RankSample(today, 400)],
            today,
            5);

        // The three missing days hold at 500 rather than dropping to rank 0.
        Assert.That(window, Is.EqualTo(new[] { 500, 500, 500, 500, 400 }));
    }

    [Test]
    public void ToDailyWindow_SeedsCarryForwardFromSamplesBeforeTheWindow()
    {
        var today = new DateOnly(2026, 7, 31);

        var window = PlayerHistorySeries.ToDailyWindow(
            [RankSample(today.AddDays(-30), 900), RankSample(today, 800)],
            today,
            3);

        Assert.That(window, Is.EqualTo(new[] { 900, 900, 800 }));
    }

    [Test]
    public void ToMonthlySeries_BackfillsFromJoinDateAndDerivesTheMonthInProgress()
    {
        var series = PlayerHistorySeries.ToMonthlySeries(
            [CountSample(new DateOnly(2026, 3, 1), 10), CountSample(new DateOnly(2026, 4, 1), 25)],
            firstMonth: new DateOnly(2026, 1, 15),
            currentMonth: new DateOnly(2026, 6, 20),
            currentCumulative: 40);

        Assert.That(series.Select(s => s.Month), Is.EqualTo(new[]
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 6, 1)
        }));

        // Jan/Feb precede any play, May has no sample, June comes from the live counter.
        Assert.That(series.Select(s => s.Count), Is.EqualTo(new[] { 0, 0, 10, 15, 0, 15 }));
    }

    [Test]
    public void ToMonthlySeries_AttributesAMissedRunToTheNextSampledMonth()
    {
        // April and May were never sampled, so the 30 plays that accrued across April, May and
        // June cannot be split apart and all land on June.
        var series = PlayerHistorySeries.ToMonthlySeries(
            [CountSample(new DateOnly(2026, 3, 1), 10), CountSample(new DateOnly(2026, 6, 1), 40)],
            firstMonth: new DateOnly(2026, 1, 1),
            currentMonth: new DateOnly(2026, 7, 20),
            currentCumulative: 50);

        Assert.That(series.Select(s => s.Count), Is.EqualTo(new[] { 0, 0, 10, 0, 0, 30, 10 }));
    }

    [Test]
    public void ToMonthlySeries_IncludesHistoryOlderThanTheRecordedJoinDate()
    {
        var series = PlayerHistorySeries.ToMonthlySeries(
            [CountSample(new DateOnly(2025, 11, 1), 5)],
            firstMonth: new DateOnly(2026, 1, 1),
            currentMonth: new DateOnly(2026, 1, 10),
            currentCumulative: 5);

        Assert.That(series[0].Month, Is.EqualTo(new DateOnly(2025, 11, 1)));
        Assert.That(series, Has.Count.EqualTo(3));
    }

    [Test]
    public void ToMonthlySeries_WithNoSamplesStillReportsTheMonthInProgress()
    {
        var series = PlayerHistorySeries.ToMonthlySeries(
            [],
            firstMonth: new DateOnly(2026, 5, 3),
            currentMonth: new DateOnly(2026, 6, 20),
            currentCumulative: 12);

        Assert.That(series.Select(s => s.Count), Is.EqualTo(new[] { 0, 12 }));
    }

    [Test]
    public void ToMonthlySeries_DoesNotDoubleCountAnAlreadySampledCurrentMonth()
    {
        var series = PlayerHistorySeries.ToMonthlySeries(
            [CountSample(new DateOnly(2026, 5, 1), 4)],
            firstMonth: new DateOnly(2026, 3, 1),
            currentMonth: new DateOnly(2026, 5, 20),
            currentCumulative: 4);

        Assert.That(series.Select(s => s.Count), Is.EqualTo(new[] { 0, 0, 4 }));
    }

    [Test]
    public void ToMonthlySeries_CanStartAtFirstActivityInsteadOfSignup()
    {
        // Cumulative stays 0 until May, so Jan-Apr carry no information.
        var samples = new[]
        {
            CountSample(new DateOnly(2026, 3, 1), 0),
            CountSample(new DateOnly(2026, 4, 1), 0),
            CountSample(new DateOnly(2026, 5, 1), 4)
        };

        var series = PlayerHistorySeries.ToMonthlySeries(
            samples,
            firstMonth: new DateOnly(2026, 1, 1),
            currentMonth: new DateOnly(2026, 6, 20),
            currentCumulative: 6,
            trimLeadingEmptyMonths: true);

        Assert.That(series.Select(s => s.Month), Is.EqualTo(new[]
        {
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 6, 1)
        }));

        Assert.That(series.Select(s => s.Count), Is.EqualTo(new[] { 4, 2 }));
    }

    [Test]
    public void ToMonthlySeries_TrimmingKeepsEmptyMonthsAfterTheFirstActivity()
    {
        var samples = new[]
        {
            CountSample(new DateOnly(2026, 3, 1), 5),
            CountSample(new DateOnly(2026, 4, 1), 5),
            CountSample(new DateOnly(2026, 5, 1), 9)
        };

        var series = PlayerHistorySeries.ToMonthlySeries(
            samples,
            firstMonth: new DateOnly(2026, 1, 1),
            currentMonth: new DateOnly(2026, 5, 20),
            currentCumulative: 9,
            trimLeadingEmptyMonths: true);

        // April is idle but sits between active months, so it stays.
        Assert.That(series.Select(s => s.Count), Is.EqualTo(new[] { 5, 0, 4 }));
    }

    [Test]
    public void ToMonthlySeries_YieldsNothingWhenThereWasNeverAnyActivity(
        [Values(false, true)] bool trimLeadingEmptyMonths
    ) {
        var samples = new[]
        {
            CountSample(new DateOnly(2026, 3, 1), 0),
            CountSample(new DateOnly(2026, 4, 1), 0),
            CountSample(new DateOnly(2026, 5, 1), 0)
        };

        var series = PlayerHistorySeries.ToMonthlySeries(
            samples,
            firstMonth: new DateOnly(2026, 1, 1),
            currentMonth: new DateOnly(2026, 6, 20),
            currentCumulative: 0,
            trimLeadingEmptyMonths);

        Assert.That(series, Is.Empty);
    }

    [Test]
    public void ToMonthlySeries_WithoutTrimmingKeepsLeadingEmptyMonthsOncePlayed()
    {
        // A single play in the current month is enough to surface the whole run since signup.
        var series = PlayerHistorySeries.ToMonthlySeries(
            [CountSample(new DateOnly(2026, 4, 1), 0)],
            firstMonth: new DateOnly(2026, 3, 1),
            currentMonth: new DateOnly(2026, 5, 20),
            currentCumulative: 1);

        Assert.That(series.Select(s => s.Count), Is.EqualTo(new[] { 0, 0, 1 }));
    }

    [Test]
    public void IsCumulative_IsDefinedForEveryMetric()
    {
        foreach (var metric in Enum.GetValues<HistoryMetric>())
            Assert.DoesNotThrow(() => metric.IsCumulative(), $"{metric} has no cumulative decision");
    }

    private static PlayerHistoryDto RankSample(DateOnly date, double value) => new()
    {
        PlayerId = PlayerId,
        Mode = Mode,
        Metric = HistoryMetric.GlobalRank,
        Granularity = HistoryGranularity.Daily,
        Date = date,
        Value = value
    };

    private static PlayerHistoryDto CountSample(DateOnly date, double cumulative) =>
        Sample(date, cumulative);

    private static PlayerHistoryDto Sample(DateOnly date, double value) => new()
    {
        PlayerId = PlayerId,
        Mode = Mode,
        Metric = HistoryMetric.PlayCount,
        Granularity = HistoryGranularity.Monthly,
        Date = date,
        Value = value
    };
}