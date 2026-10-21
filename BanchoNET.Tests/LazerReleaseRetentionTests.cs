using System.Text.Json;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;

namespace BanchoNET.Tests;

[TestFixture]
public class LazerReleaseRetentionTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "novemo-retention-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, true);

    private async Task<LazerReleaseBundle> CreateRelease(
        int revision,
        string channel
    ) {
        var release = new LazerReleaseBundle
        {
            Version = $"2026.1020.{revision}-{channel}",
            Channel = channel,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-30).AddMinutes(revision)
        };
        var directory = Path.Combine(_directory, release.Version);
        if (revision > 1)
        {
            var previous = (await PublishedLazerReleases.List(_directory)).FirstOrDefault();
            if (previous != null)
            {
                foreach (var platform in new[] { "win", "linux" })
                    release.Bases.Add(new LazerReleaseBase { Platform = platform, Version = previous.Version });
            }
        }
        Directory.CreateDirectory(directory);

        foreach (var platform in new[] { "win", "linux" })
        {
            foreach (var kind in new[] { "full", "delta", "feed", "portable" })
            {
                var file = new LazerReleaseFile { Name = $"{platform}-{kind}", Platform = platform, Kind = kind };
                release.Files.Add(file);
                await File.WriteAllTextAsync(Path.Combine(directory, file.Name), "payload");
            }
        }

        await File.WriteAllTextAsync(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(release));

        return release;
    }

    [Test]
    public async Task BothCurrentTargetsAndDeltaWindowsSurviveCleanup()
    {
        for (var revision = 1; revision <= 25; revision++)
            await CreateRelease(revision, revision == 5 ? "lazer" : "tachyon");

        var now = DateTimeOffset.UtcNow;
        await LazerReleaseRetention.Cleanup(_directory, now);

        var obsolete = Path.Combine(_directory, "2026.1020.6-tachyon", "win-full");
        Assert.That(File.Exists(obsolete), Is.True);
        await LazerReleaseRetention.Cleanup(_directory, now.AddDays(6));
        Assert.That(File.Exists(obsolete), Is.True);
        await LazerReleaseRetention.Cleanup(_directory, now.AddDays(8));
        await LazerReleaseRetention.Cleanup(_directory, now.AddDays(9));

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(obsolete), Is.False);
            Assert.That(File.Exists(Path.Combine(_directory, "2026.1020.5-lazer", "win-full")), Is.True);
            Assert.That(File.Exists(Path.Combine(_directory, "2026.1020.25-tachyon", "linux-portable")), Is.True);
            Assert.That(File.Exists(Path.Combine(_directory, "2026.1020.1-tachyon", "win-delta")), Is.True);
            Assert.That(File.Exists(Path.Combine(_directory, "2026.1020.6-tachyon", "win-delta")), Is.False);
            Assert.That(File.Exists(Path.Combine(_directory, "2026.1020.16-tachyon", "linux-delta")), Is.True);
            Assert.That(File.Exists(Path.Combine(_directory, "2026.1020.6-tachyon", "win-feed")), Is.True);
        });

        var releases = await PublishedLazerReleases.List(_directory);
        Assert.That(releases, Has.Count.EqualTo(25));
    }

    [Test]
    public async Task StableHistoryIncludesInterveningPatchesButExcludesNewerTachyon()
    {
        await CreateRelease(1, "lazer");
        await CreateRelease(2, "tachyon");
        await CreateRelease(3, "lazer");
        await CreateRelease(4, "tachyon");

        var releases = await PublishedLazerReleases.List(_directory);
        var stable = PublishedLazerReleases.ForStream(releases, "lazer");
        var tachyon = PublishedLazerReleases.ForStream(releases, "tachyon");

        Assert.Multiple(() =>
        {
            Assert.That(stable.Select(r => r.Version), Is.EqualTo(new[] { "2026.1020.3-lazer", "2026.1020.2-tachyon", "2026.1020.1-lazer" }));
            Assert.That(tachyon, Has.Count.EqualTo(4));
            Assert.That(tachyon[0].Version, Is.EqualTo("2026.1020.4-tachyon"));
        });
    }

    [Test]
    public void InboxCleanupOnlyRemovesAcknowledgedExpiredSubmissions()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var name in new[] { "pending.ready", "failed.ready", "completed.ready", "recent.ready" })
        {
            var directory = Path.Combine(_directory, name);
            Directory.CreateDirectory(directory);
            if (name == "pending.ready") continue;

            var receipt = Path.Combine(directory, "result.json");
            File.WriteAllText(receipt, JsonSerializer.Serialize(new { Success = name != "failed.ready" }));
            File.SetLastWriteTimeUtc(receipt, now.AddDays(name == "recent.ready" ? -1 : -8).UtcDateTime);
        }

        LazerReleaseRetention.CleanupInbox(_directory, now);

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(Path.Combine(_directory, "pending.ready")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_directory, "failed.ready")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(_directory, "completed.ready")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(_directory, "recent.ready")), Is.True);
        });
    }

    [Test]
    public async Task MissingOrExcessiveDeltaChainsUseFullUpdates()
    {
        var first = await CreateRelease(1, "lazer");
        await CreateRelease(2, "tachyon");
        await CreateRelease(3, "lazer");
        var releases = await PublishedLazerReleases.List(_directory);

        Assert.Multiple(() =>
        {
            Assert.That(PublishedLazerReleases.CanUseDeltas(_directory, releases, first.Version), Is.True);
            Assert.That(PublishedLazerReleases.CanUseDeltas(_directory, releases, first.Version, 1), Is.False);
            Assert.That(PublishedLazerReleases.CanUseDeltas(_directory, releases, "none"), Is.False);
            Assert.That(PublishedLazerReleases.CanUseDeltas(_directory, releases, "2026.1019.1-lazer"), Is.False);
        });

        var delta = releases[0].Files.First(f => f.Kind == "delta");
        var path = Path.Combine(_directory, releases[0].Version, delta.Name);
        File.Delete(path);

        Assert.That(PublishedLazerReleases.CanUseDeltas(_directory, releases, first.Version), Is.False);
        await File.WriteAllTextAsync(path, "payload");
        releases[1].Bases.Clear();

        Assert.That(PublishedLazerReleases.CanUseDeltas(_directory, releases, first.Version), Is.False);
    }
}
