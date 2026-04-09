using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;
using Cronos;

namespace BanchoNET.Services;

public class LazerUpdaterService(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory
) : BackgroundService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(nameof(LazerUpdaterService));
    
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        try
        {
            await UpdateLazer(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ignore during startup
        }
        catch (Exception ex)
        {
            logger.LogError("Error while trying to update lazer files", ex, caller: nameof(LazerUpdaterService));
        }

        // everyday at midnight
        var cron = CronExpression.Parse("0 0 * * *");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var next = cron.GetNextOccurrence(now, TimeZoneInfo.Utc);

                if (next == null)
                {
                    logger.LogWarning("Failed to get next cron occurence for lazer updater", caller: nameof(LazerUpdaterService));
                    return;
                }

                var delay = next.Value - now;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);
                
                await UpdateLazer(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError("Error while updating lazer files", ex, caller: nameof(LazerUpdaterService));
            }
        }
    }

    private async Task UpdateLazer(
        CancellationToken ct
    ) {
        if (string.IsNullOrEmpty(AppSettings.GithubToken))
            return;
        
        var lazerVersion = string.Empty;
        
        if (File.Exists(LazerStorage.CurrentLazerVersionFile))
        {
            var lazerVersions = (await File.ReadAllTextAsync(LazerStorage.CurrentLazerVersionFile, ct)).Split('\n');

            lazerVersion = lazerVersions.Length > 0 ? lazerVersions[0] : string.Empty;
        }
        
        var latestReleases = await GetLatestMatchingReleases(ct);
        
        var latestLazerRelease = latestReleases.FirstOrDefault(r => r.TagName.EndsWith("-lazer"));
        var shouldUpdate = latestLazerRelease != null && !latestLazerRelease.TagName.Equals(lazerVersion);
        
        if (shouldUpdate)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var releases = scope.ServiceProvider.GetRequiredService<IReleasesRepository>();

            var tagName = latestLazerRelease!.TagName;
            var success = await TryReplaceRepo(tachyon: false, tagName, ct);
            if (success)
            {
                var latest = await LatestRelease(tagName, ct);
                await releases.InsertRelease(prerelease: false, latest.Full, latest.Delta);
            }
            
            var latestLazerReleaseIndex = latestReleases.IndexOf(latestLazerRelease);
            
            for (var i = latestLazerReleaseIndex - 1; i >= 0; i--)
            {
                tagName = latestReleases[i].TagName;
                var isTachyon = tagName.Contains("-tachyon");
                success = await TryReplaceRepo(tachyon: isTachyon, tagName, ct);
                if (success)
                {
                    var latest = await LatestRelease(tagName, ct);
                    await releases.InsertRelease(prerelease: isTachyon, latest.Full, latest.Delta);
                }
            }
        }
        else
        {
            if (shouldUpdate) logger.LogWarning("Failed to fetch lazer releases");
            else logger.LogInfo("Lazer files are up to date");

            return;
        }
        
        var latestTachyonVersion = latestReleases.FirstOrDefault(r => r.TagName.EndsWith("-tachyon"))?.TagName;

        await File.WriteAllTextAsync(
            LazerStorage.CurrentLazerVersionFile, $"{latestLazerRelease.TagName}\n{latestTachyonVersion}", ct
        );
    }

    private static async Task<(VelopackAsset Full, VelopackAsset? Delta)> LatestRelease(
        string tagName,
        CancellationToken ct
    ) {
        await using var fs = File.OpenRead(LazerStorage.ReleasesPath);
        var feed = await JsonSerializer.DeserializeAsync<VelopackReleaseFeed>(fs, cancellationToken: ct)
                   ?? throw new InvalidOperationException($"Unable to deserialize {LazerStorage.ReleasesPath}");

        var latestFull = feed.Assets.First(a => a.Type.Equals("Full", StringComparison.OrdinalIgnoreCase));
        var latestDelta = feed.Assets.FirstOrDefault(a => a.Type.Equals("Delta", StringComparison.OrdinalIgnoreCase));

        return (latestFull, latestDelta);
    }

    private async Task<bool> TryReplaceRepo(
        bool tachyon,
        string tagName,
        CancellationToken ct
    ) {
        await ReplaceRepo(tachyon, tagName, ct);
        await ReplaceDomains(ct);
        await UpdateCsproj(ct);

        var result = await RunCommand(
            "/opt/dotnet8/dotnet",
            $"publish --self-contained -r win-x64 -p:Version={tagName} -o publish",
            LazerStorage.LazerProjectPath,
            ct
        );
        if (result.ExitCode != 0)
        {
            logger.LogWarning("Failed to update lazer files");
            logger.LogWarning(result.StdErr);
            return false;
        }

        result = await RunCommand(
            "/opt/tools/vpk",
            $"[win] pack --runtime win-x64 --packTitle {AppSettings.LazerName} -u {AppSettings.LazerName} -i \"{LazerStorage.IconPath}\" -v {tagName} -p \"{LazerStorage.LazerPublishPath}\" -e \"osu!.exe\"",
            Storage.LazerPath,
            ct
        );
        if (result.ExitCode != 0)
        {
            logger.LogWarning("Failed to pack lazer update");
            logger.LogWarning(result.StdErr);
            return false;
        }
        
        logger.LogInfo($"Updated lazer files to latest version: {tagName}");
        return true;
    }
    
    private static async Task<(int ExitCode, string StdErr)> RunCommand(
        string filename,
        string arguments,
        string workingDirectory,
        CancellationToken ct
    ) {
        var psi = new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = psi;
        process.EnableRaisingEvents = true;

        var stderr = new StringBuilder();
        
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stderr.AppendLine(e.Data);
        };
        
        if (!process.Start())
            throw new InvalidOperationException($"Failed to start process: {filename}");
        
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);

        return (process.ExitCode, stderr.ToString());
    }

    private async Task<List<GitHubRelease>> GetLatestMatchingReleases(
        CancellationToken ct
    ) {
        var releases = new List<GitHubRelease>();

        var lazerFound = false;
        var tachyonFound = false;
        for (var page = 1; page <= 10; page++)
        {
            var url = $"https://api.github.com/repos/ppy/osu/releases?per_page=10&page={page}";
            using var doc = await GetJson(url, ct);

            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                break;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var rel = GitHubRelease.FromJson(item);
                if (!lazerFound && rel is { Prerelease: false, Draft: false })
                {
                    lazerFound = true;
                    releases.Add(rel);
                }
                if (!tachyonFound && rel is { Prerelease: true, Draft: false })
                {
                    tachyonFound = true;
                    releases.Add(rel);
                }
            }
            
            if (lazerFound && tachyonFound)
                break;
        }

        return releases;
    }

    private static async Task UpdateCsproj(
        CancellationToken ct
    ) {
        await ReplaceInFile(
            LazerStorage.OsuDesktopCsproj,
            "</Project>",
            "\t<ItemGroup>\n\t<AssemblyAttribute Include=\"osu.Game.Utils.OfficialBuildAttribute\" />\n\t</ItemGroup>\n</Project>",
            ct
        );
    }

    private async Task ReplaceRepo(
        bool tachyon,
        string tagName,
        CancellationToken ct
    ) {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ghrel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var zipPath = Path.Combine(tempRoot, "src.zip");
            var zipUrl = $"https://api.github.com/repos/ppy/osu/zipball/{Uri.EscapeDataString(tagName)}";
            await DownloadRepo(zipUrl, zipPath, ct);

            var extractDir = Path.Combine(tempRoot, "extract");
            Directory.CreateDirectory(extractDir);
            await ZipFile.ExtractToDirectoryAsync(zipPath, extractDir, ct);

            var root = Directory.GetDirectories(extractDir).Single();

            var targetDirectory = LazerStorage.LazerPath;
            if (Directory.Exists(targetDirectory))
                Directory.Delete(targetDirectory, recursive: true);

            Directory.CreateDirectory(targetDirectory);
            CopyDirectoryContents(root, targetDirectory);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static void CopyDirectoryContents(
        string sourceDir,
        string targetDir
    ) {
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(targetDir, rel));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var dest = Path.Combine(targetDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private async Task<JsonDocument> GetJson(
        string url,
        CancellationToken ct
    ) {
        
        using var response = await _client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private async Task DownloadRepo(
        string url,
        string path,
        CancellationToken ct
    ) {
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(path);
        await input.CopyToAsync(output, ct);
    }

    private static async Task ReplaceDomains(
        CancellationToken ct
    ) {
        // Update production endpoint to match ours
        var text = await File.ReadAllTextAsync(LazerStorage.ProductionEndpointPath, ct);
        
        text = Regex.Replace(text, @"ppy\.sh\b", AppSettings.Domain);
        text = Regex.Replace(text, @"spectator\.osu\b", "spectator");
        
        await File.WriteAllTextAsync(LazerStorage.ProductionEndpointPath, text, ct);

        // Make lazer trust our hostname
        await ReplaceInFile(
            LazerStorage.TrustedDomainStorePath,
            "if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !uri.Host.EndsWith(@\".ppy.sh\", StringComparison.OrdinalIgnoreCase))",
            $"if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !(uri.Host.EndsWith(@\".ppy.sh\", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(@\".{AppSettings.Domain}\", StringComparison.OrdinalIgnoreCase)))",
            ct
        );
        
        // Change updater url to use our internal one
        await ReplaceInFile(LazerStorage.VelopackUpdaterPath, "github.com", $"api.{AppSettings.Domain}", ct);
        
        //TODO this might be removed, check it once in a while
        await ReplaceInFile(LazerStorage.DrawableAvatarPath, "ppy.sh", AppSettings.Domain, ct);
    }

    private static async Task ReplaceInFile(
        string path,
        string source,
        string target,
        CancellationToken ct
    ) {
        var text = await File.ReadAllTextAsync(path, ct);
        text = text.Replace(source, target);
        await File.WriteAllTextAsync(path, text, ct);
    }
    
    private sealed record GitHubRelease(string TagName, bool Draft, bool Prerelease)
    {
        public static GitHubRelease FromJson(JsonElement e)
        {
            return new GitHubRelease(
                TagName: e.GetProperty("tag_name").GetString() ?? "",
                Draft: e.GetProperty("draft").GetBoolean(),
                Prerelease: e.GetProperty("prerelease").GetBoolean());
        }
    }
}