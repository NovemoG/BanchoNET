using System.Text.Json;
using BanchoNET.Core.Models;

namespace BanchoNET.Core.Utils;

public static class LazerReleasePublisher
{
    public static async Task Import(
        string directory,
        string root,
        string packageId,
        CancellationToken ct = default
    ) {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0
            || (File.GetAttributes(Path.Combine(directory, "manifest.json")) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Release directory and manifest must not be links.");
        }

        var release = await LazerReleaseBundle.Read(directory, ct);
        await release.Validate(directory, packageId, ct);

        Directory.CreateDirectory(root);

        var destination = Path.Combine(root, release.Version);
        if (Directory.Exists(destination))
        {
            var existing = await LazerReleaseBundle.Read(destination, ct);
            if (existing.Commit != release.Commit
                || existing.SchemaVersion != release.SchemaVersion
                || existing.Channel != release.Channel
                || existing.PackageId != release.PackageId
                || JsonSerializer.Serialize(existing.Bases) != JsonSerializer.Serialize(release.Bases)
                || JsonSerializer.Serialize(existing.Files) != JsonSerializer.Serialize(release.Files))
            {
                throw new InvalidDataException("Version exists with different content. Use a new version.");
            }

            return;
        }

        var published = await PublishedLazerReleases.List(root, ct);
        var latest = published.FirstOrDefault();
        
        if (latest != null && LazerReleaseBundle.VersionNumber(release.Version) <= LazerReleaseBundle.VersionNumber(latest.Version))
            throw new InvalidDataException("Version must exceed the latest published release.");

        foreach (var deltaBase in release.Bases)
        {
            if (latest == null || deltaBase.Version != latest.Version
                || !deltaBase.SHA256.Equals(latest.Single(deltaBase.Platform, "full").SHA256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Delta base is stale or does not match the published package. Build a new candidate.");
            }
        }

        if (release.SchemaVersion == 2 && latest != null && release.Bases.Count == 0)
            throw new InvalidDataException("A published base exists; delta packages are required.");

        var staging = Path.Combine(root, ".import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);

        try
        {
            foreach (var file in release.Files)
            {
                await using var input = File.OpenRead(Path.Combine(directory, file.Name));
                await using var output = File.Create(Path.Combine(staging, file.Name));

                await input.CopyToAsync(output, ct);
            }

            await release.Validate(staging, packageId, ct);
            release.PublishedAt = DateTimeOffset.UtcNow;

            await File.WriteAllTextAsync(Path.Combine(staging, "manifest.json"), JsonSerializer.Serialize(release), ct);
            Directory.Move(staging, destination);
        }
        finally
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, true);
        }
    }
}