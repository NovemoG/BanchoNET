using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BanchoNET.Core.Utils;

public static class BeatmapArchive
{
    public static string GetVersion(
        IReadOnlyDictionary<int, string> checksums
    ) {
        var entries = checksums.OrderBy(entry => entry.Key)
            .Select(entry => $"{entry.Key}:{entry.Value.ToUpperInvariant()}");

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', entries))));
    }

    public static async Task<bool> IsCurrent(
        string path,
        IReadOnlyDictionary<int, string> checksums,
        CancellationToken cancellationToken = default
    ) {
        if (!File.Exists(path) || checksums.Count == 0)
            return false;

        var version = GetVersion(checksums);
        var file = new FileInfo(path);

        try
        {
            var json = await File.ReadAllTextAsync(path + ".json", cancellationToken);
            var manifest = JsonSerializer.Deserialize<Manifest>(json);
            if (manifest != null && manifest.Length == file.Length
                && manifest.LastWriteTimeUtc == file.LastWriteTimeUtc)
            {
                return manifest.Version == version;
            }
        }
        catch (IOException) { }
        catch (JsonException) { }

        if (!await Matches(path, checksums, cancellationToken))
            return false;

        await WriteManifest(path, version, cancellationToken);
        return true;
    }

    public static async Task<bool> Matches(
        string path,
        IReadOnlyDictionary<int, string> checksums,
        CancellationToken cancellationToken = default
    ) {
        if (checksums.Count == 0)
            return false;

        try
        {
            await using var archive = await ZipFile.OpenReadAsync(path, cancellationToken);
            var entries = archive.Entries
                .Where(entry => entry.FullName.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (entries.Length != checksums.Count)
                return false;

            var remaining = checksums.Values.ToList();
            foreach (var entry in entries)
            {
                await using var stream = await entry.OpenAsync(cancellationToken);
                var hash = Convert.ToHexString(await MD5.HashDataAsync(stream, cancellationToken));
                var index = remaining.FindIndex(value => value.Equals(hash, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                    return false;

                remaining.RemoveAt(index);
            }

            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public static async Task Publish(
        string tempPath,
        string path,
        string version,
        CancellationToken cancellationToken = default
    ) {
        File.Delete(path + ".json");
        File.Move(tempPath, path, overwrite: true);
        await WriteManifest(path, version, cancellationToken);
    }

    private static async Task WriteManifest(
        string path,
        string version,
        CancellationToken cancellationToken
    ) {
        var file = new FileInfo(path);
        var manifest = new Manifest(version, file.Length, file.LastWriteTimeUtc);
        var tempPath = path + $".{Guid.NewGuid()}.json";

        try
        {
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(manifest), cancellationToken);
            File.Move(tempPath, path + ".json", overwrite: true);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private sealed record Manifest(string Version, long Length, DateTime LastWriteTimeUtc);
}