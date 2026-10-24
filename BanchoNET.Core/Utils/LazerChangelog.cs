using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Changelog;

namespace BanchoNET.Core.Utils;

public static class LazerChangelog
{
    private static readonly (string Name, string DisplayName)[] Streams = [("lazer", "Lazer"), ("tachyon", "Tachyon")];

    public static ChangelogIndexResponse Index(
        List<LazerReleaseBundle> releases
    ) {
        var result = new ChangelogIndexResponse();

        for (var i = 0; i < Streams.Length; i++)
        {
            var latest = releases.FirstOrDefault(r => r.Channel == Streams[i].Name);
            if (latest == null) continue;

            var stream = Stream(i);
            stream.LatestBuild = Build(latest, Stream(i));
            result.Streams.Add(stream);
        }

        result.Builds = releases
            .Where(r => result.Streams.Any(s => s.Name == r.Channel))
            .Select(r => Build(r, result.Streams.First(s => s.Name == r.Channel), Entries(releases, r)))
            .ToList();

        return result;
    }

    public static ChangelogBuild? Single(
        List<LazerReleaseBundle> releases,
        string stream,
        string version
    ) {
        var index = Array.FindIndex(Streams, s => s.Name == stream);
        if (index < 0) return null;

        var inStream = releases.Where(r => r.Channel == stream).ToList();
        var position = inStream.FindIndex(r => r.Version == version + "-" + stream);
        if (position < 0) return null;

        var build = Build(inStream[position], Stream(index), Entries(releases, inStream[position]));
        build.Versions = new ChangelogVersions
        {
            // Releases are ordered newest first.
            Next = position > 0 ? Build(inStream[position - 1], Stream(index)) : null,
            Previous = position + 1 < inStream.Count ? Build(inStream[position + 1], Stream(index)) : null
        };

        return build;
    }

    private static ChangelogStream Stream(
        int index
    ) => new()
    {
        Id = index + 1,
        Name = Streams[index].Name,
        DisplayName = Streams[index].DisplayName,
        IsFeatured = index == 0
    };
    
    private static List<LazerChangelogEntry> Entries(
        List<LazerReleaseBundle> releases,
        LazerReleaseBundle release
    ) {
        if (release.Channel != "lazer") return release.Changelog;

        var older = releases
            .SkipWhile(r => r != release)
            .Skip(1)
            .TakeWhile(r => r.Channel != "lazer")
            .Reverse();

        return older.Append(release)
            .SelectMany(r => r.Changelog)
            .DistinctBy(e => (e.Type, e.Category.ToLowerInvariant(), e.Title.ToLowerInvariant()))
            .ToList();
    }

    private static ChangelogBuild Build(
        LazerReleaseBundle release,
        ChangelogStream stream,
        List<LazerChangelogEntry>? entries = null
    ) {
        var id = BuildId(release.Version);

        return new ChangelogBuild
        {
            Id = id,
            Version = release.Version.Split('-')[0],
            DisplayVersion = release.Version.Split('-')[0],
            CreatedAt = release.PublishedAt,
            UpdateStream = stream,
            ChangelogEntries = (entries ?? []).Select((e, i) => new ChangelogEntry
            {
                Id = id * 1000L + i,
                Type = e.Type,
                Category = e.Category,
                Title = e.Title,
                Message = e.Message,
                Major = e.Major,
                CreatedAt = release.PublishedAt
            }).ToList()
        };
    }
    
    public static int BuildId(
        string version
    ) {
        var number = LazerReleaseBundle.VersionNumber(version);

        return number.Major % 100 * 10_000_000 + number.Minor * 1_000 + number.Build;
    }

    public static LazerReleaseBundle? FindBuild(
        List<LazerReleaseBundle> releases,
        int buildId
    ) => releases.FirstOrDefault(r => BuildId(r.Version) == buildId);

    public static string DisplayName(
        LazerReleaseBundle release
    ) => $"{Streams.First(s => s.Name == release.Channel).DisplayName} {release.Version.Split('-')[0]}";
}