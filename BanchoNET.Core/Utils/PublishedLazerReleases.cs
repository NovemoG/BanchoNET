using BanchoNET.Core.Models;

namespace BanchoNET.Core.Utils;

public static class PublishedLazerReleases
{
    public static string Root => Path.Combine(LazerStorage.ReleasesPath, "Published");

    public static Task<List<LazerReleaseBundle>> List(
        CancellationToken ct = default
    ) => List(Root, ct);

    public static async Task<List<LazerReleaseBundle>> List(
        string root,
        CancellationToken ct = default
    ) {
        var result = new List<LazerReleaseBundle>();
        if (!Directory.Exists(root)) return result;

        foreach (var path in Directory.EnumerateDirectories(root))
        {
            if (!LazerReleaseBundle.IsVersion(Path.GetFileName(path))) continue;

            result.Add(await LazerReleaseBundle.Read(path, ct));
        }

        return result
            .OrderByDescending(r => LazerReleaseBundle.VersionNumber(r.Version))
            .ToList();
    }

    public static List<LazerReleaseBundle> ForStream(
        List<LazerReleaseBundle> releases,
        string stream
    ) {
        var target = releases.FirstOrDefault(r => stream == "tachyon" || r.Channel == "lazer");

        return target == null ? [] : releases
            .Where(r => LazerReleaseBundle.VersionNumber(r.Version) <= LazerReleaseBundle.VersionNumber(target.Version))
            .ToList();
    }

    public static string FilePath(
        LazerReleaseBundle release,
        LazerReleaseFile file
    ) => Path.Combine(Root, release.Version, file.Name);

    public static bool CanUseDeltas(
        string root,
        List<LazerReleaseBundle> releases,
        string? baseVersion,
        int maximumDeltas = 10
    ) {
        if (baseVersion == null || !LazerReleaseBundle.IsVersion(baseVersion)) return false;

        var current = releases.FirstOrDefault();
        var count = 0;

        while (current != null && current.Version != baseVersion)
        {
            if (++count > maximumDeltas || current.Bases.Count != 2
                || current.Bases.Select(b => b.Version).Distinct().Count() != 1
                || current.Files.Count(f => f.Kind == "delta") != 2
                || current.Files.Where(f => f.Kind == "delta")
                    .Any(f => !File.Exists(Path.Combine(root, current.Version, f.Name))))
            {
                return false;
            }

            current = releases.FirstOrDefault(r => r.Version == current.Bases[0].Version);
        }

        return current != null;
    }
}