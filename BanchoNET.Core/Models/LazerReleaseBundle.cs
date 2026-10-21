using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BanchoNET.Core.Models;

public sealed class LazerReleaseBundle
{
    public int SchemaVersion { get; set; } = 1;
    public string Version { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public string Commit { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; }
    public List<LazerReleaseFile> Files { get; set; } = [];
    public List<LazerReleaseBase> Bases { get; set; } = [];

    public static bool IsVersion(
        string value
    ) => Regex.IsMatch(value, @"^\d{4}\.\d{3,4}\.\d+-(lazer|tachyon)$");

    public static Version VersionNumber(
        string value
    ) => System.Version.Parse(value.Split('-')[0]);

    public static async Task<LazerReleaseBundle> Read(
        string directory,
        CancellationToken ct = default
    ) {
        await using var input = File.OpenRead(Path.Combine(directory, "manifest.json"));

        return await JsonSerializer.DeserializeAsync<LazerReleaseBundle>(input, cancellationToken: ct)
               ?? throw new InvalidDataException("Missing release manifest.");
    }

    public async Task Validate(
        string directory,
        string packageId,
        CancellationToken ct = default
    ) {
        if (SchemaVersion is not (1 or 2) || !IsVersion(Version) || Channel is not ("lazer" or "tachyon")
            || !Version.EndsWith("-" + Channel, StringComparison.Ordinal) || PackageId != packageId)
        {
            throw new InvalidDataException("Invalid release version, channel, schema or package ID.");
        }

        if (Files.Count != 7 + Bases.Count || Bases.Count is not (0 or 2)
            || (SchemaVersion == 1 && Bases.Count != 0)
            || Bases.Select(b => b.Platform).Distinct().Count() != Bases.Count
            || Files.Count(f => f.Kind == "delta") != Bases.Count
            || Files.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Files.Count
            || Files.GroupBy(f => (f.Platform, f.Kind)).Any(g => g.Count() != 1))
        {
            throw new InvalidDataException("Invalid release files or delta bases.");
        }

        foreach (var file in Files)
        {
            if (file.Platform is not ("win" or "linux") || file.Kind is not ("feed" or "full" or "delta" or "portable" or "setup")
                                                        || string.IsNullOrEmpty(file.Name)
                                                        || file.Name != Path.GetFileName(file.Name)
                                                        || file.Name.IndexOfAny(['/', '\\', ':']) >= 0
                                                        || file.Name is "." or ".." or "manifest.json" or "result.json" or "result.tmp")
            {
                throw new InvalidDataException("Invalid release file name or role.");
            }

            var path = Path.Combine(directory, file.Name);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || new FileInfo(path).Length != file.Size)
                throw new InvalidDataException($"Invalid file or size: {file.Name}");

            await using var stream = File.OpenRead(path);

            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
            if (!hash.Equals(file.SHA256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Checksum mismatch: {file.Name}");
        }
        foreach (var platform in new[] { "win", "linux" })
        {
            var feedFile = Single(platform, "feed");
            var full = Single(platform, "full");
            _ = Single(platform, "portable");
            
            var delta = Files.SingleOrDefault(f => f.Platform == platform && f.Kind == "delta");
            var deltaBase = Bases.SingleOrDefault(b => b.Platform == platform);
            
            if ((delta == null) != (deltaBase == null) 
                || deltaBase != null 
                && (!IsVersion(deltaBase.Version) 
                    || VersionNumber(deltaBase.Version) >= VersionNumber(Version) 
                    || !Regex.IsMatch(deltaBase.SHA256, "^[a-fA-F0-9]{64}$")))
            {
                throw new InvalidDataException("Invalid delta base.");
            }

            if (feedFile.Name != $"releases.{platform}.json")
                throw new InvalidDataException("The installed updater channels must remain win/linux.");

            await using var stream = File.OpenRead(Path.Combine(directory, feedFile.Name));

            var feed = await JsonSerializer.DeserializeAsync<VelopackReleaseFeed>(stream, cancellationToken: ct);
            if (feed?.Assets.Count != (delta == null ? 1 : 2)
                || feed.Assets.Count(a => a.Type == "Full") != 1
                || feed.Assets.Count(a => a.Type == "Delta") != (delta == null ? 0 : 1))
            {
                throw new InvalidDataException("Invalid full/delta feed assets.");
            }

            foreach (var asset in feed.Assets)
            {
                var file = asset.Type == "Full" ? full : delta!;
                if (asset.Version != Version
                    || asset.FileName != file.Name
                    || asset.Size != file.Size
                    || !asset.SHA256.Equals(file.SHA256, StringComparison.OrdinalIgnoreCase)
                    || asset.PackageId != PackageId)
                {
                    throw new InvalidDataException("Feed does not match release manifest.");
                }
            }
        }
        _ = Single("win", "setup");
        if (Files.Any(f => f is { Platform: "linux", Kind: "setup" }))
            throw new InvalidDataException("Unexpected Linux setup file.");

        if (Bases.Any(b => b.Platform is not ("win" or "linux")) || Bases.Select(b => b.Version).Distinct().Count() > 1)
            throw new InvalidDataException("Invalid delta platforms or versions.");
    }

    public async Task<VelopackReleaseFeed> ReadFeed(
        string directory,
        string platform,
        bool fullOnly = false,
        CancellationToken ct = default
    ) {
        await using var input = File.OpenRead(Path.Combine(directory, Single(platform, "feed").Name));

        var feed = await JsonSerializer.DeserializeAsync<VelopackReleaseFeed>(input, cancellationToken: ct)
                   ?? throw new InvalidDataException("Missing release feed.");
        feed.Assets = feed.Assets.Where(a => (!fullOnly || a.Type == "Full")
            && Files.Any(f => f.Name == a.FileName && File.Exists(Path.Combine(directory, f.Name)))).ToList();

        return feed;
    }

    public LazerReleaseFile Single(
        string platform,
        string kind
    ) => Files.Single(f => f.Platform == platform && f.Kind == kind);
}

public sealed class LazerReleaseBase
{
    public string Platform { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string SHA256 { get; set; } = string.Empty;
}

public sealed class LazerReleaseFile
{
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SHA256 { get; set; } = string.Empty;
}