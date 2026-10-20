using BanchoNET.Core.Models;

namespace BanchoNET.Core.Utils;

public static class PublishedLazerReleases
{
    public static string Root => Path.Combine(LazerStorage.ReleasesPath, "Published");

    public static async Task<List<LazerReleaseBundle>> List(
        CancellationToken ct = default
    ) {
        var result = new List<LazerReleaseBundle>();
        if (!Directory.Exists(Root)) return result;

        foreach (var path in Directory.EnumerateDirectories(Root))
        {
            if (!LazerReleaseBundle.IsVersion(Path.GetFileName(path))) continue;

            result.Add(await LazerReleaseBundle.Read(path, ct));
        }

        return result
            .OrderByDescending(r => r.PublishedAt)
            .ToList();
    }

    public static string FilePath(
        LazerReleaseBundle release,
        LazerReleaseFile file
    ) => Path.Combine(Root, release.Version, file.Name);
}