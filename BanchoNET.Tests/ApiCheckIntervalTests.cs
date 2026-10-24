using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Tests;

public class ApiCheckIntervalTests
{
    [Test]
    public void RecentlyUpdatedSet_UsesMinimumInterval()
    {
        var interval = BeatmapExtensions.GetApiCheckInterval(DateTime.UtcNow.AddHours(-3));

        Assert.That(interval, Is.EqualTo(BeatmapExtensions.MinimumApiCheckInterval));
    }

    [Test]
    public void Interval_IsTenthOfTimeSinceUpdate()
    {
        var interval = BeatmapExtensions.GetApiCheckInterval(DateTime.UtcNow.AddDays(-100));

        Assert.That(interval.TotalDays, Is.EqualTo(10).Within(0.01));
    }

    [Test]
    public void LongUntouchedSet_IsCappedAtMaximumInterval()
    {
        var interval = BeatmapExtensions.GetApiCheckInterval(new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.That(interval, Is.EqualTo(BeatmapExtensions.MaximumApiCheckInterval));
    }
}
