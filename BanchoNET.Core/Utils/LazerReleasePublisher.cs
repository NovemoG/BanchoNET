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
                || existing.Channel != release.Channel
                || existing.PackageId != release.PackageId
                || JsonSerializer.Serialize(existing.Files) != JsonSerializer.Serialize(release.Files))
            {
                throw new InvalidDataException("Version exists with different content. Use a new version.");
            }

            return;
        }

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