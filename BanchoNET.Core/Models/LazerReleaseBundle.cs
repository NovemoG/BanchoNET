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

    public static bool IsVersion(
        string value
    ) => Regex.IsMatch(value, @"^\d{4}\.\d{3,4}\.\d+-(lazer|tachyon)$");

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
        if (SchemaVersion != 1 || !IsVersion(Version) || Channel is not ("lazer" or "tachyon")
            || !Version.EndsWith("-" + Channel, StringComparison.Ordinal) || PackageId != packageId)
        {
            throw new InvalidDataException("Invalid release version, channel, schema or package ID.");
        }

        if (Files.Count != 7 ||
            Files.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Files.Count)
        {
            throw new InvalidDataException("Expected seven uniquely named release files (full packages only).");
        }

        foreach (var file in Files)
        {
            if (file.Platform is not ("win" or "linux") || file.Kind is not ("feed" or "full" or "portable" or "setup")
                                                        || string.IsNullOrEmpty(file.Name)
                                                        || file.Name != Path.GetFileName(file.Name)
                                                        || file.Name.IndexOfAny(['/', '\\', ':']) >= 0
                                                        || file.Name is "." or ".." or "manifest.json")
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

            if (feedFile.Name != $"releases.{platform}.json")
                throw new InvalidDataException("The installed updater channels must remain win/linux.");

            await using var stream = File.OpenRead(Path.Combine(directory, feedFile.Name));

            var feed = await JsonSerializer.DeserializeAsync<VelopackReleaseFeed>(stream, cancellationToken: ct);
            if (feed?.Assets.Count != 1) throw new InvalidDataException("Expected one full package in each feed.");

            var asset = feed.Assets[0];
            if (asset.Type != "Full"
                || asset.Version != Version
                || asset.FileName != full.Name
                || asset.Size != full.Size
                || !asset.SHA256.Equals(full.SHA256, StringComparison.OrdinalIgnoreCase)
                || asset.PackageId != PackageId)
            {
                throw new InvalidDataException("Feed does not match release manifest.");
            }
        }
        _ = Single("win", "setup");
        if (Files.Any(f => f is { Platform: "linux", Kind: "setup" }))
            throw new InvalidDataException("Unexpected Linux setup file.");
    }

    public LazerReleaseFile Single(
        string platform,
        string kind
    ) => Files.Single(f => f.Platform == platform && f.Kind == kind);
}

public sealed class LazerReleaseFile
{
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SHA256 { get; set; } = string.Empty;
}