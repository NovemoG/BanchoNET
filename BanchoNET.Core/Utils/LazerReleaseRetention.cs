using System.Text.Json;
using BanchoNET.Core.Models;

namespace BanchoNET.Core.Utils;

public static class LazerReleaseRetention
{
    public static async Task Cleanup(
        string root,
        DateTimeOffset now,
        CancellationToken ct = default
    ) {
        var releases = await PublishedLazerReleases.List(root, ct);
        if (releases.Count == 0) return;

        var targets = new[] { releases[0], releases.FirstOrDefault(r => r.Channel == "lazer") }
            .OfType<LazerReleaseBundle>()
            .Distinct()
            .ToList();
        var keep = new HashSet<string>();

        foreach (var target in targets)
        {
            foreach (var file in target.Files)
                keep.Add(Path.Combine(target.Version, file.Name));

            foreach (var release in releases
                .Where(r => LazerReleaseBundle.VersionNumber(r.Version) <= LazerReleaseBundle.VersionNumber(target.Version))
                .Take(10))
            {
                foreach (var file in release.Files.Where(f => f.Kind == "delta"))
                    keep.Add(Path.Combine(release.Version, file.Name));
            }
        }

        var statePath = Path.Combine(root, ".retired.json");
        var retired = File.Exists(statePath)
            ? JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(await File.ReadAllTextAsync(statePath, ct))
              ?? throw new InvalidDataException("Invalid release retention state.")
            : new Dictionary<string, DateTimeOffset>();

        foreach (var release in releases)
        {
            foreach (var file in release.Files.Where(f => f.Kind != "feed"))
            {
                ct.ThrowIfCancellationRequested();

                var key = Path.Combine(release.Version, file.Name);
                var path = Path.Combine(root, key);
                if (keep.Contains(key) || !File.Exists(path))
                {
                    retired.Remove(key);
                    continue;
                }

                if (!retired.TryGetValue(key, out var since))
                {
                    retired[key] = now;
                    continue;
                }

                if (now - since < TimeSpan.FromDays(7)) continue;

                File.Delete(path);
                retired.Remove(key);
            }
        }

        var temporary = statePath + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(retired), ct);
        File.Move(temporary, statePath, true);
    }

    public static void CleanupInbox(
        string inbox,
        DateTimeOffset now
    ) {
        var candidates = Path.Combine(inbox, "candidates");
        var directories = Directory.EnumerateDirectories(inbox, "*.ready")
            .Concat(Directory.Exists(candidates)
                ? Directory.EnumerateDirectories(candidates).Where(p => LazerReleaseBundle.IsVersion(Path.GetFileName(p)))
                : []);

        foreach (var directory in directories)
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) continue;

            var receipt = Path.Combine(directory, "result.json");
            if (!File.Exists(receipt) || now - File.GetLastWriteTimeUtc(receipt) < TimeSpan.FromDays(7)) continue;

            using var result = JsonDocument.Parse(File.ReadAllText(receipt));
            if (result.RootElement.GetProperty("Success").GetBoolean())
                Directory.Delete(directory, true);
        }
    }
}